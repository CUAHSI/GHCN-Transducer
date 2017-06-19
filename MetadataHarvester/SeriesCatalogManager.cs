using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace MetadataHarvester
{
    class SeriesCatalogManager
    {
        private Dictionary<string, GhcnSite> _siteLookup;

        private Dictionary<string, GhcnVariable> _variableLookup;

        public SeriesCatalogManager()
        {
            // initialize the site and variable lookup
            _siteLookup = getSiteLookup();
            _variableLookup = getVariableLookup();

        }

        private Dictionary<string, GhcnSite> getSiteLookup()
        {
            Dictionary<string, GhcnSite> lookup = new Dictionary<string, GhcnSite>();

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connString))
            {               
                string sql = "SELECT SiteID, SiteCode, SiteName FROM dbo.Sites";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Connection.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string code = reader.GetString(1);
                        GhcnSite site = new GhcnSite
                        {
                            SiteID = reader.GetInt32(0),
                            SiteCode = code,
                            SiteName = reader.GetString(2),  
                        };
                        lookup.Add(code, site);
                    }
                    reader.Close();
                    cmd.Connection.Close();
                }   
            }
            return lookup;
        }


        private Dictionary<string, GhcnVariable> getVariableLookup()
        {
            Dictionary<string, GhcnVariable> lookup = new Dictionary<string, GhcnVariable>();

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                string sql = "SELECT VariableID, VariableCode, VariableName FROM dbo.Variables";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Connection.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string code = reader.GetString(1);
                        GhcnVariable variable = new GhcnVariable
                        {
                            VariableID = reader.GetInt32(0),
                            VariableCode = code,
                            VariableName = reader.GetString(2),
                        };
                        lookup.Add(code, variable);
                    }
                    reader.Close();
                    cmd.Connection.Close();
                }
            }
            return lookup;
        }

        public List<GhcnSeries> ReadSeriesFromInventory()
        {
            Console.WriteLine("Reading Series from GHCN file ghcnd-inventory.txt ...");

            List<GhcnSeries> seriesList = new List<GhcnSeries>();
            Dictionary<string, TextFileColumn> colPos = new Dictionary<string,TextFileColumn>();
            colPos.Add("sitecode", new TextFileColumn(1, 11));
            colPos.Add("varcode", new TextFileColumn(32, 35));
            colPos.Add("firstyear", new TextFileColumn(37, 40));
            colPos.Add("lastyear", new TextFileColumn(42, 45));

            string url = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-inventory.txt";

            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string siteCode = line.Substring(colPos["sitecode"].Start, colPos["sitecode"].Length);
                    string varCode = line.Substring(colPos["varcode"].Start, colPos["varcode"].Length);
                    int firstYear = Convert.ToInt32(line.Substring(colPos["firstyear"].Start, colPos["firstyear"].Length));
                    int lastYear = Convert.ToInt32(line.Substring(colPos["lastyear"].Start, colPos["lastyear"].Length));

                    DateTime beginDateTime = new DateTime(firstYear, 1, 1);
                    DateTime endDateTime = new DateTime(lastYear, 12, 31);
                    if (lastYear == DateTime.Now.Year)
                    {
                        endDateTime = (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).AddDays(-1);
                    }
                    int valueCount = (int)((endDateTime - beginDateTime).TotalDays);

                    // only add series for the GHCN core variables (SNWD, PRCP, TMAX, TMIN, TAVG)
                    if (_variableLookup.ContainsKey(varCode) && _siteLookup.ContainsKey(siteCode))
                    {
                        seriesList.Add(new GhcnSeries
                        {
                            SiteCode = siteCode,
                            SiteID = _siteLookup[siteCode].SiteID,
                            SiteName = _siteLookup[siteCode].SiteName,
                            VariableCode = varCode,
                            VariableID = _variableLookup[varCode].VariableID,
                            BeginDateTime = beginDateTime,
                            EndDateTime = endDateTime,
                            ValueCount = valueCount
                        });
                    }
                }
            }
            Console.WriteLine(String.Format("found {0} series", seriesList.Count));
            return seriesList;
        }

        private void SaveOrUpdateSeries(GhcnSeries series, Dictionary<Tuple<int, long>, long> lookup, SqlConnection connection)
        {
            Tuple<int, long> seriesKey = new Tuple<int, long>(series.VariableID, series.SiteID);

            if (!lookup.ContainsKey(seriesKey))
            {

                string sql = @"INSERT INTO dbo.SeriesCatalog(
                                SiteID, 
                                SiteCode, 
                                SiteName,
                                VariableID, 
                                VariableCode, 
                                BeginDateTime, 
                                EndDateTime,
                                ValueCount)
                            VALUES(
                                @SiteID, 
                                @SiteCode,
                                @SiteName, 
                                @VariableID, 
                                @VariableCode, 
                                @BeginDateTime, 
                                @EndDateTime, 
                                @ValueCount)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@SiteID", series.SiteID));
                        cmd.Parameters.Add(new SqlParameter("@SiteCode", series.SiteCode));
                        cmd.Parameters.Add(new SqlParameter("@SiteName", series.SiteName));
                        cmd.Parameters.Add(new SqlParameter("@VariableID", series.VariableID));
                        cmd.Parameters.Add(new SqlParameter("@VariableCode", series.VariableCode));
                        cmd.Parameters.Add(new SqlParameter("@BeginDateTime", series.BeginDateTime));
                        cmd.Parameters.Add(new SqlParameter("@EndDateTime", series.EndDateTime));
                        cmd.Parameters.Add(new SqlParameter("@ValueCount", series.ValueCount));
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error inserting series SiteID=" + series.SiteID.ToString() + "VariableID=" + series.VariableID.ToString() +  " " + ex.Message);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                long seriesID = lookup[seriesKey];

                string sql = @"UPDATE dbo.SeriesCatalog SET
                                BeginDateTime = @BeginDateTime, 
                                EndDateTime = @EndDateTime,
                                ValueCount = @ValueCount
                            WHERE
                                SeriesID = @SeriesID";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@BeginDateTime", series.BeginDateTime));
                        cmd.Parameters.Add(new SqlParameter("@EndDateTime", series.EndDateTime));
                        cmd.Parameters.Add(new SqlParameter("@ValueCount", series.ValueCount));
                        cmd.Parameters.Add(new SqlParameter("@SeriesID", seriesID));
                        cmd.ExecuteNonQuery();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error updating series ID=" + seriesID.ToString() + " " + ex.Message);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }


        private Dictionary<Tuple<int, long>, long> GetSeriesLookup(SqlConnection connection)
        {
            // lookup (VariableID, SiteID => SeriesID)
            Dictionary<Tuple<int, long>, long> lookup = new Dictionary<Tuple<int, long>, long>();

            using (SqlCommand cmd = new SqlCommand("SELECT VariableID, SiteID, SeriesID FROM dbo.SeriesCatalog", connection))
            {
                connection.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var item = new Tuple<int, long>(Convert.ToInt32(r["VariableID"]), Convert.ToInt64(r["SiteID"]));
                        lookup.Add(item, Convert.ToInt64(r["SeriesID"]));
                    }
                }
                connection.Close();
            }
            return lookup;
        }


        public void UpdateSeriesCatalog_fast()
        {
            List<GhcnSeries> seriesList = ReadSeriesFromInventory();
            Console.WriteLine("updating series catalog for " + seriesList.Count.ToString() + " series ...");

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                // delete old entries from series catalog
                string sql = "TRUNCATE TABLE dbo.SeriesCatalog";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("deleted old series from SeriesCatalog");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error deleting old series from SeriesCatalog");
                    }
                    finally
                    {
                        connection.Close();
                    }
                }

                int batchSize = 500;
                int numBatches = (seriesList.Count / batchSize) + 1;
                for(int b = 0; b < numBatches; b++)
                {
                    // prepare for bulk insert
                    DataTable bulkTable = new DataTable();
                    bulkTable.Columns.Add("SiteID", typeof(long));
                    bulkTable.Columns.Add("VariableID", typeof(int));
                    bulkTable.Columns.Add("SiteCode", typeof(string));
                    bulkTable.Columns.Add("VariableCode", typeof(string));
                    bulkTable.Columns.Add("MethodID", typeof(int));
                    bulkTable.Columns.Add("SourceID", typeof(int));
                    bulkTable.Columns.Add("QualityControlLevelID", typeof(int));
                    bulkTable.Columns.Add("BeginDateTime", typeof(DateTime));
                    bulkTable.Columns.Add("EndDateTime", typeof(DateTime));
                    bulkTable.Columns.Add("ValueCount", typeof(int));

                    int batchStart = b * batchSize;
                    int batchEnd = batchStart + batchSize;
                    if (batchEnd >= seriesList.Count)
                    {
                        batchEnd = seriesList.Count; 
                    }
                    for (int i = batchStart; i < batchEnd; i++)
                    {
                        var row = bulkTable.NewRow();
                        row["SiteID"] = seriesList[i].SiteID;
                        row["VariableID"] = seriesList[i].VariableID;
                        row["SiteCode"] = seriesList[i].SiteCode;
                        row["VariableCode"] = seriesList[i].VariableCode;
                        row["MethodID"] = 0;
                        row["SourceID"] = 1;
                        row["QualityControlLevelID"] = 1;
                        row["BeginDateTime"] = seriesList[i].BeginDateTime;
                        row["EndDateTime"] = seriesList[i].EndDateTime;
                        row["ValueCount"] = seriesList[i].ValueCount;
                        bulkTable.Rows.Add(row);
                    }
                    SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
                    bulkCopy.DestinationTableName = "dbo.SeriesCatalog";
                    connection.Open();
                    bulkCopy.WriteToServer(bulkTable);
                    connection.Close();
                    Console.WriteLine("SeriesCatalog inserted row " + batchEnd.ToString());
                }

                
                    
                // series catalog lookup for better speed..
                // Dictionary<Tuple<int, long>, long> lookup = GetSeriesLookup(connection);
            }
        }


        public void UpdateSeriesCatalog()
        {
            List<GhcnSeries> seriesList = ReadSeriesFromInventory();
            Console.WriteLine("updating series catalog for " + seriesList.Count.ToString() + " series ...");

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            int n = seriesList.Count;
            int i = 0;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                // series catalog lookup for better speed..
                Dictionary<Tuple<int, long>, long> lookup = GetSeriesLookup(connection);


                foreach (GhcnSeries series in seriesList)
                {
                    SaveOrUpdateSeries(series, lookup, connection);
                    i++;
                    if (i % 1000 == 0)
                    {
                        Console.WriteLine("SaveOrUpdateSeries " + Convert.ToString(i));
                    }
                }
            }
        }

    }
}
