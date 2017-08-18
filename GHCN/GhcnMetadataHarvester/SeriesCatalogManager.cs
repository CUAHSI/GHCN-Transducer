using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace GhcnHarvester
{
    class SeriesCatalogManager
    {
        private LogWriter _log;
        private Dictionary<string, GhcnVariable> _variableLookup;
        

        public SeriesCatalogManager(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
            _variableLookup = getVariableLookup();
        }

        /// <summary>
        /// Create a dictionary for looking up a GhcnSite object based on its site code
        /// </summary>
        /// <returns>SiteCode - GhcnSite object lookup dictionary</returns>
        private Dictionary<string, GhcnSite> GetSiteLookup()
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


        /// <summary>
        /// Get Source details from the ODM
        /// For GHCN we have only one source
        /// </summary>
        /// <returns>SiteCode - GhcnSite object lookup dictionary</returns>
        private GhcnSource GetSource()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            GhcnSource source = new GhcnSource();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                string sql = "SELECT TOP 1 SourceID, SourceCode, Organization, SourceDescription, Citation FROM dbo.Sources";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Connection.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string code = reader.GetString(1);
                        source = new GhcnSource
                        {
                            SourceID = reader.GetInt32(0),
                            SourceCode = reader.GetString(1),
                            Organization = reader.GetString(2),
                            SourceDescription = reader.GetString(3),
                            Citation = reader.GetString(4)
                        };
                    }
                    reader.Close();
                    cmd.Connection.Close();
                }
            }
            return source;
        }

        /// <summary>
        /// find the method description from the ODM (for inserting into SeriesCatalog)
        /// </summary>
        /// <returns></returns>
        private string GetMethodDescription()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            string methodDesc = "Unknown";

            using (SqlConnection connection = new SqlConnection(connString))
            {
                string sql = "SELECT TOP 1 MethodDescription FROM dbo.Methods";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Connection.Open();

                    var resultObj = cmd.ExecuteScalar();
                    methodDesc = Convert.ToString(resultObj);

                    cmd.Connection.Close();
                }
            }
            return methodDesc;
        }


        /// <summary>
        /// Create a dictionary for looking up a GhcnVariable object based on its variable code
        /// </summary>
        /// <returns>Variable code - GhcnVariable object lookup dictionary</returns>
        private Dictionary<string, GhcnVariable> getVariableLookup()
        {
            Dictionary<string, GhcnVariable> lookup = new Dictionary<string, GhcnVariable>();
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            try
            {

                using (SqlConnection connection = new SqlConnection(connString))
                {
                    string sql = "SELECT VariableID, VariableCode, VariableName, VariableUnitsID, SampleMedium, DataType FROM dbo.Variables";
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
                                VariableUnitsID = reader.GetInt32(3),
                                SampleMedium = reader.GetString(4),
                                DataType = reader.GetString(5)
                            };
                            lookup.Add(code, variable);
                        }
                        reader.Close();
                        cmd.Connection.Close();
                    }
                }
            }
            catch(Exception ex)
            {
                _log.LogWrite("SeriesCatalogManager ERROR in GetVariableLookup: " + ex.Message);
            }
            return lookup;
        }

        /// <summary>
        /// Reads a list of time series (observed variable and period of record) from the online GHCND inventory
        /// </summary>
        /// <param name="siteLookup">Lookup dictionary to find site object by site code</param>
        /// <returns>List of Series objects with info on Site, Variable, Start date and End date</returns>
        public List<GhcnSeries> ReadSeriesFromInventory(Dictionary<string, GhcnSite> siteLookup)
        {
            Console.WriteLine("Reading Series from GHCN file ghcnd-inventory.txt ...");
            
            List<GhcnSeries> seriesList = new List<GhcnSeries>();
            Dictionary<string, TextFileColumn> colPos = new Dictionary<string,TextFileColumn>();
            colPos.Add("sitecode", new TextFileColumn(1, 11));
            colPos.Add("varcode", new TextFileColumn(32, 35));
            colPos.Add("firstyear", new TextFileColumn(37, 40));
            colPos.Add("lastyear", new TextFileColumn(42, 45));

            string url = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-inventory.txt";
            _log.LogWrite("Reading Series from url: " + url);

            try
            {

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
                        if (_variableLookup.ContainsKey(varCode) && siteLookup.ContainsKey(siteCode))
                        {
                            seriesList.Add(new GhcnSeries
                            {
                                SiteCode = siteCode,
                                SiteID = siteLookup[siteCode].SiteID,
                                SiteName = siteLookup[siteCode].SiteName,
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
                _log.LogWrite(String.Format("found {0} series", seriesList.Count));
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSeriesCatalog ERROR reading series from web: " + ex.Message);
            }
            return seriesList;
        }

        /// <summary>
        /// Update ODM SeriesCatalog using SQL bulk insert method
        /// </summary>
        public void UpdateSeriesCatalog_fast()
        {
            var siteLookup = GetSiteLookup();

            var source = GetSource();
            string methodDescription = GetMethodDescription();

            List<GhcnSeries> seriesList = ReadSeriesFromInventory(siteLookup);
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
                        _log.LogWrite("UpdateSeriesCatalog: error deleting old series from SeriesCatalog");
                        return;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }

                int batchSize = 500;
                int numBatches = (seriesList.Count / batchSize) + 1;
                long seriesID = 0L;

                try
                {
                    for (int b = 0; b < numBatches; b++)
                    {
                        // prepare for bulk insert
                        DataTable bulkTable = new DataTable();
                        bulkTable.Columns.Add("SeriesID", typeof(long));
                        bulkTable.Columns.Add("SiteID", typeof(long));
                        bulkTable.Columns.Add("SiteCode", typeof(string));
                        bulkTable.Columns.Add("SiteName", typeof(string));
                        bulkTable.Columns.Add("SiteType", typeof(string));
                        bulkTable.Columns.Add("VariableID", typeof(int));
                        bulkTable.Columns.Add("VariableCode", typeof(string));
                        bulkTable.Columns.Add("VariableName", typeof(string));
                        bulkTable.Columns.Add("Speciation", typeof(string));
                        bulkTable.Columns.Add("VariableUnitsID", typeof(int));
                        bulkTable.Columns.Add("VariableUnitsName", typeof(string));
                        bulkTable.Columns.Add("SampleMedium", typeof(string));
                        bulkTable.Columns.Add("ValueType", typeof(string));
                        bulkTable.Columns.Add("TimeSupport", typeof(float));
                        bulkTable.Columns.Add("TimeUnitsID", typeof(int));
                        bulkTable.Columns.Add("TimeUnitsName", typeof(string));
                        bulkTable.Columns.Add("DataType", typeof(string));
                        bulkTable.Columns.Add("GeneralCategory", typeof(string));
                        bulkTable.Columns.Add("MethodID", typeof(int));
                        bulkTable.Columns.Add("MethodDescription", typeof(string));
                        bulkTable.Columns.Add("SourceID", typeof(int));
                        bulkTable.Columns.Add("Organization", typeof(string));
                        bulkTable.Columns.Add("SourceDescription", typeof(string));
                        bulkTable.Columns.Add("Citation", typeof(string));
                        bulkTable.Columns.Add("QualityControlLevelID", typeof(int));
                        bulkTable.Columns.Add("QualityControlLevelCode", typeof(string));
                        bulkTable.Columns.Add("BeginDateTime", typeof(DateTime));
                        bulkTable.Columns.Add("EndDateTime", typeof(DateTime));
                        bulkTable.Columns.Add("BeginDateTimeUTC", typeof(DateTime));
                        bulkTable.Columns.Add("EndDateTimeUTC", typeof(DateTime));
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
                            seriesID = seriesID + 1;
                            GhcnVariable v = _variableLookup[seriesList[i].VariableCode];
                            row["SeriesID"] = seriesID;
                            row["SiteID"] = seriesList[i].SiteID;
                            row["SiteCode"] = seriesList[i].SiteCode;
                            row["SiteName"] = seriesList[i].SiteName;
                            row["SiteType"] = "Atmosphere";
                            row["VariableID"] = seriesList[i].VariableID;
                            row["VariableCode"] = v.VariableCode;
                            row["VariableName"] = v.VariableName;
                            row["Speciation"] = v.Speciation;
                            row["VariableUnitsID"] = v.VariableUnitsID;
                            row["VariableUnitsName"] = "Centimeter"; // todo get from DB!!
                            row["SampleMedium"] = v.SampleMedium;
                            row["ValueType"] = v.ValueType;
                            row["TimeSupport"] = v.TimeSupport;
                            row["TimeUnitsID"] = v.TimeUnitsID;
                            row["TimeUnitsName"] = "Day"; // todo get from DB!!
                            row["DataType"] = v.DataType;
                            row["GeneralCategory"] = v.GeneralCategory;
                            row["MethodID"] = 0;
                            row["MethodDescription"] = methodDescription;
                            row["SourceID"] = 1;
                            row["Organization"] = source.Organization;
                            row["SourceDescription"] = source.SourceDescription;
                            row["Citation"] = source.Citation;
                            row["QualityControlLevelID"] = 1;
                            row["QualityControlLevelCode"] = "1";
                            row["BeginDateTime"] = seriesList[i].BeginDateTime;
                            row["EndDateTime"] = seriesList[i].EndDateTime;
                            row["BeginDateTimeUTC"] = seriesList[i].BeginDateTime;
                            row["EndDateTimeUTC"] = seriesList[i].EndDateTime;
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
                    Console.WriteLine("UpdateSeriesCatalog: " + seriesList.Count.ToString() + " series updated.");
                    _log.LogWrite("UpdateSeriesCatalog: " + seriesList.Count.ToString() + " series updated.");
                }
                catch(Exception ex)
                {
                    _log.LogWrite("UpdateSeriesCatalog ERROR: " + ex.Message);
                }
            }
        }
    }
}
