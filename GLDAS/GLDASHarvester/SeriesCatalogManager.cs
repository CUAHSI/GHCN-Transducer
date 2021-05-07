using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace GldasHarvester
{
    class SeriesCatalogManager
    {
        private LogWriter _log;

        public SeriesCatalogManager(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
        }

        /// <summary>
        /// Create a dictionary for looking up a GhcnSite object based on its site code
        /// </summary>
        /// <returns>SiteCode - GhcnSite object lookup dictionary</returns>
        private Dictionary<string, GldasSite> GetSiteLookup()
        {
            Dictionary<string, GldasSite> lookup = new Dictionary<string, GldasSite>();

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connString))
            {               
                string sql = "SELECT SiteID, SiteCode, SiteName, Longitude, Latitude FROM dbo.Sites";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Connection.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string code = reader.GetString(1);
                        var longitude = reader.GetValue(3);
                        var latitude = reader.GetValue(4);
                        var siteId = Convert.ToInt64(reader.GetValue(0));
                        GldasSite site = new GldasSite(siteId, Convert.ToSingle(latitude), Convert.ToSingle(longitude));
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
        private GldasSource GetSource()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
           GldasSource source = new GldasSource();

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
                        source = new GldasSource
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


        private Dictionary<string, Variable> GetVariableLookup()
        {
            var lookup = new Dictionary<string, Variable>();

            var variableList = new List<Variable>();

            // fetch information from ODM (Variables joined with Units table)
            string sqlQuery = @"SELECT v.VariableID, v.VariableCode,  v.VariableName, 
v.SampleMedium,v.TimeUnitsID, v.DataType, v.GeneralCategory, v.VariableUnitsID, v.ValueType, v.Speciation, 
tu.UnitsName AS TimeUnitsName, u.UnitsName AS VariableUnitsName 
FROM dbo.Variables v 
INNER JOIN dbo.Units u ON v.VariableUnitsID = u.UnitsID
INNER JOIN dbo.Units tu ON v.TimeUnitsID = tu.UnitsID";

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    using (var cmd = new SqlCommand(sqlQuery, connection))
                    {

                        connection.Open();
                        var reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var v = new Variable();
                                var variableCode = Convert.ToString(reader["VariableCode"]);
                                v.VariableID = Convert.ToInt32(reader["VariableID"]);
                                v.VariableCode = variableCode;
                                v.VariableName = Convert.ToString(reader["VariableName"]);
                                v.SampleMedium = Convert.ToString(reader["SampleMedium"]);
                                v.DataType = Convert.ToString(reader["DataType"]);
                                v.VariableUnitsID = Convert.ToInt32(reader["VariableUnitsID"]);
                                v.VariableUnitsName = Convert.ToString(reader["VariableUnitsName"]);
                                lookup.Add(variableCode, v);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "ERROR reading product-variable lookup from DB: " + ex.Message;
                Console.WriteLine(msg);
                _log.LogWrite(msg);
                return null;
            }
            return lookup;
        }


        /// <summary>
        /// Reads a list of time series (observed variable and period of record) from the online GHCND inventory
        /// </summary>
        /// <param name="siteLookup">Lookup dictionary to find site object by site code</param>
        /// <returns>List of Series objects with info on Site, Variable, Start date and End date</returns>
        public List<GldasSeries> GenerateSeriesList(Dictionary<string, GldasSite> siteLookup, Dictionary<string, Variable> variableLookup)
        {
            Console.WriteLine("Reading Series from database ...");

            List<GldasSeries> seriesList = new List<GldasSeries>();

            var beginDateTime = new DateTime(2000, 1, 1);
            var endDateTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            // Total number of days * number of 3-hour datapoints in a day.
            var valueCount = Convert.ToInt32(endDateTime.Subtract(beginDateTime).TotalDays * 8);

            foreach (string siteCode in siteLookup.Keys)
            {
                foreach (string varCode in variableLookup.Keys)
                {
                    seriesList.Add(new GldasSeries
                    {
                        SiteCode = siteCode,
                        SiteID = siteLookup[siteCode].SiteID,
                        SiteName = siteLookup[siteCode].SiteName,
                        VariableCode = varCode,
                        VariableID = variableLookup[varCode].VariableID,
                        BeginDateTime = beginDateTime,
                        EndDateTime = endDateTime,
                        ValueCount = valueCount
                    });
                }
            }
            Console.WriteLine(String.Format("found {0} series", seriesList.Count));
            _log.LogWrite(String.Format("found {0} series", seriesList.Count));

            return seriesList;
        }

        /// <summary>
        /// Update ODM SeriesCatalog using SQL bulk insert method
        /// </summary>
        public void UpdateSeriesCatalog_fast()
        {
            var siteLookup = GetSiteLookup();
            var variableLookup = GetVariableLookup();

            var source = GetSource();
            string methodDescription = GetMethodDescription();

            List<GldasSeries> seriesList = GenerateSeriesList(siteLookup, variableLookup);
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
                            Variable v = variableLookup[seriesList[i].VariableCode];
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
                            row["VariableUnitsName"] = v.VariableUnitsName;
                            row["SampleMedium"] = v.SampleMedium;
                            row["ValueType"] = v.ValueType;
                            row["TimeSupport"] = v.TimeSupport;
                            row["TimeUnitsID"] = v.TimeUnitsID;
                            row["TimeUnitsName"] = v.TimeUnitsName;
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
