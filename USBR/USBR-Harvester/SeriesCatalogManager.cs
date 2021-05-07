using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace USBRHarvester
{
    class SeriesCatalogManager
    {
        private LogWriter _log;
        private Dictionary<string, Variable> _variableLookup;


        public SeriesCatalogManager(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
            _variableLookup = getVariableLookup();
        }

        private Dictionary<string, Site> GetSiteLookup()
        {
            var lookup = new Dictionary<string, Site>();

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
                        var site = new Site
                        {
                            SiteID = Convert.ToInt32(reader["SiteID"]),
                            SiteCode = Convert.ToString(reader["SiteCode"]),
                            SiteName = Convert.ToString(reader["SiteName"])
                        };
                        lookup.Add(code, site);
                    }
                    reader.Close();
                    cmd.Connection.Close();
                }
            }
            return lookup;
        }


        private Dictionary<string, Variable> getVariableLookup()
        {
            Dictionary<string, Variable> lookup = new Dictionary<string, Variable>();
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            try
            {

                using (SqlConnection connection = new SqlConnection(connString))
                {
                    string sql = "SELECT VariableID, VariableCode, VariableName, VariableUnitsID, SampleMedium, DataType, TimeSupport, TimeUnitsID FROM dbo.Variables";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Connection.Open();

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            string code = reader.GetString(1);
                            var variable = new Variable
                            {
                                VariableID = reader.GetInt32(0),
                                VariableCode = code,
                                VariableName = reader.GetString(2),
                                VariableUnitsID = reader.GetInt32(3),
                                SampleMedium = reader.GetString(4),
                                DataType = reader.GetString(5),
                                TimeSupport = Convert.ToSingle(reader.GetValue(6)),
                                TimeUnitsID = reader.GetInt32(7)
                 
                            };
                            lookup.Add(code, variable);
                        }
                        reader.Close();
                        cmd.Connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("SeriesCatalogManager ERROR in GetVariableLookup: " + ex.Message);
            }
            return lookup;
        }

        private Dictionary<string, MethodInfo> getMethodLookup()
        {
            Dictionary<string, MethodInfo> lookup = new Dictionary<string, MethodInfo>();
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            try
            {

                using (SqlConnection connection = new SqlConnection(connString))
                {
                    string sql = "SELECT MethodID, MethodDescription, MethodLink FROM dbo.Methods";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Connection.Open();

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            var m = new MethodInfo
                            {
                                MethodID = reader.GetInt32(0),
                                MethodDescription = reader.GetString(1),
                                MethodLink = reader.GetString(2)
                            };
                            lookup.Add(m.MethodLink, m);
                        }
                        reader.Close();
                        cmd.Connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("SeriesCatalogManager ERROR in GetMethodLookup: " + ex.Message);
            }
            return lookup;
        }


        private Source getSource()
        {
            // getting the first Source from the Sources table
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            var s = new Source();
            try
            {

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string sql = "SELECT SourceID, Organization, SourceDescription, Citation FROM dbo.Sources";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Connection.Open();

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            s = new Source
                            {
                                SourceID = reader.GetInt32(0),
                                Organization = reader.GetString(1),
                                SourceDescription = reader.GetString(2),
                                Citation = reader.GetString(3)
                            };
                        }
                        reader.Close();
                        cmd.Connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("SeriesCatalogManager ERROR in GetMethodLookup: " + ex.Message);
            }
            return s;
        }


        //public void UpdateSeriesCatalog_fast()
        //{
        //    var siteLookup = GetSiteLookup();
        //    var variableLookup = getVariableLookup();
        //    var methodLookup = getMethodLookup();
        //    var source = getSource();

        //    var wsClient = new AwdbClient(_log);
        //    List<Series> seriesList = wsClient.GetAllSeries();
            
        //    Console.WriteLine("updating series catalog for " + seriesList.Count.ToString() + " series ...");

        //    string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
        //    using (SqlConnection connection = new SqlConnection(connString))
        //    {
        //        // delete old entries from series catalog
        //        string sql = "TRUNCATE TABLE dbo.SeriesCatalog";
        //        using (SqlCommand cmd = new SqlCommand(sql, connection))
        //        {
        //            try
        //            {
        //                connection.Open();
        //                cmd.ExecuteNonQuery();
        //                Console.WriteLine("deleted old series from SeriesCatalog");
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("error deleting old series from SeriesCatalog");
        //                _log.LogWrite("UpdateSeriesCatalog: error deleting old series from SeriesCatalog");
        //                return;
        //            }
        //            finally
        //            {
        //                connection.Close();
        //            }
        //        }

        //        int batchSize = 500;
        //        int numBatches = (seriesList.Count / batchSize) + 1;
        //        long seriesID = 0L;

        //        try
        //        {
        //            for (int b = 0; b < numBatches; b++)
        //            {
        //                // prepare for bulk insert
        //                DataTable bulkTable = new DataTable();
        //                bulkTable.Columns.Add("SeriesID", typeof(long));
        //                bulkTable.Columns.Add("SiteID", typeof(long));
        //                bulkTable.Columns.Add("SiteCode", typeof(string));
        //                bulkTable.Columns.Add("SiteName", typeof(string));
        //                bulkTable.Columns.Add("SiteType", typeof(string));
        //                bulkTable.Columns.Add("VariableID", typeof(int));
        //                bulkTable.Columns.Add("VariableCode", typeof(string));
        //                bulkTable.Columns.Add("VariableName", typeof(string));
        //                bulkTable.Columns.Add("Speciation", typeof(string));
        //                bulkTable.Columns.Add("VariableUnitsID", typeof(int));
        //                bulkTable.Columns.Add("VariableUnitsName", typeof(string));
        //                bulkTable.Columns.Add("SampleMedium", typeof(string));
        //                bulkTable.Columns.Add("ValueType", typeof(string));
        //                bulkTable.Columns.Add("TimeSupport", typeof(float));
        //                bulkTable.Columns.Add("TimeUnitsID", typeof(int));
        //                bulkTable.Columns.Add("TimeUnitsName", typeof(string));
        //                bulkTable.Columns.Add("DataType", typeof(string));
        //                bulkTable.Columns.Add("GeneralCategory", typeof(string));
        //                bulkTable.Columns.Add("MethodID", typeof(int));
        //                bulkTable.Columns.Add("MethodDescription", typeof(string));
        //                bulkTable.Columns.Add("SourceID", typeof(int));
        //                bulkTable.Columns.Add("Organization", typeof(string));
        //                bulkTable.Columns.Add("SourceDescription", typeof(string));
        //                bulkTable.Columns.Add("Citation", typeof(string));
        //                bulkTable.Columns.Add("QualityControlLevelID", typeof(int));
        //                bulkTable.Columns.Add("QualityControlLevelCode", typeof(string));
        //                bulkTable.Columns.Add("BeginDateTime", typeof(DateTime));
        //                bulkTable.Columns.Add("EndDateTime", typeof(DateTime));
        //                bulkTable.Columns.Add("BeginDateTimeUTC", typeof(DateTime));
        //                bulkTable.Columns.Add("EndDateTimeUTC", typeof(DateTime));
        //                bulkTable.Columns.Add("ValueCount", typeof(int));

        //                int batchStart = b * batchSize;
        //                int batchEnd = batchStart + batchSize;
        //                if (batchEnd >= seriesList.Count)
        //                {
        //                    batchEnd = seriesList.Count;
        //                }
        //                for (int i = batchStart; i < batchEnd; i++)
        //                {
        //                    try
        //                    {
        //                        var variableCode = seriesList[i].VariableCode;
        //                        var siteCode = seriesList[i].SiteCode;
        //                        var methodCode = seriesList[i].MethodCode;

        //                        // only include series for variables which are already in the Variables table
        //                        // for example "DIAG" series are excluded because the "DIAG" variable has no CUAHSI equivalent ...
        //                        if (_variableLookup.ContainsKey(variableCode) && siteLookup.ContainsKey(siteCode) && methodLookup.ContainsKey(methodCode))
        //                        {
        //                            var row = bulkTable.NewRow();
        //                            seriesID = seriesID + 1;
        //                            Variable v = _variableLookup[variableCode];
        //                            Site s = siteLookup[siteCode];
        //                            MethodInfo m = methodLookup[methodCode];

        //                            row["SeriesID"] = seriesID;
        //                            row["SiteID"] = s.SiteID;
        //                            row["SiteCode"] = seriesList[i].SiteCode;
        //                            row["SiteName"] = s.SiteName;
        //                            row["SiteType"] = "Atmosphere";
        //                            row["VariableID"] = v.VariableID;
        //                            row["VariableCode"] = v.VariableCode;
        //                            row["VariableName"] = v.VariableName;
        //                            row["Speciation"] = v.Speciation;
        //                            row["VariableUnitsID"] = v.VariableUnitsID;
        //                            row["VariableUnitsName"] = v.VariableUnitsName;
        //                            row["SampleMedium"] = v.SampleMedium;
        //                            row["ValueType"] = v.ValueType;
        //                            row["TimeSupport"] = v.TimeSupport;
        //                            row["TimeUnitsID"] = v.TimeUnitsID;
        //                            row["TimeUnitsName"] = "Day"; // todo get from DB !!!
        //                            row["DataType"] = v.DataType;
        //                            row["GeneralCategory"] = v.GeneralCategory;
        //                            row["MethodID"] = m.MethodID;
        //                            row["MethodDescription"] = m.MethodDescription;
        //                            row["SourceID"] = source.SourceID;
        //                            row["Organization"] = source.Organization;
        //                            row["SourceDescription"] = source.SourceDescription;
        //                            row["Citation"] = source.Citation;
        //                            row["QualityControlLevelID"] = 1;
        //                            row["QualityControlLevelCode"] = "1";
        //                            row["BeginDateTime"] = seriesList[i].BeginDateTime;
        //                            row["EndDateTime"] = seriesList[i].EndDateTime;
        //                            row["BeginDateTimeUTC"] = seriesList[i].BeginDateTime;
        //                            row["EndDateTimeUTC"] = seriesList[i].EndDateTime;
        //                            row["ValueCount"] = seriesList[i].ValueCount;
        //                            bulkTable.Rows.Add(row);
        //                        }
        //                    }
        //                    catch(Exception ex)
        //                    {
        //                        Console.WriteLine(ex.Message);
        //                    }
        //                }
        //                SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
        //                bulkCopy.DestinationTableName = "dbo.SeriesCatalog";
        //                connection.Open();
        //                bulkCopy.WriteToServer(bulkTable);
        //                connection.Close();
        //                Console.WriteLine("SeriesCatalog inserted row " + batchEnd.ToString());
        //            }
        //            Console.WriteLine("UpdateSeriesCatalog: " + seriesList.Count.ToString() + " series updated.");
        //            _log.LogWrite("UpdateSeriesCatalog: " + seriesList.Count.ToString() + " series updated.");
        //        }
        //        catch(Exception ex)
        //        {
        //            _log.LogWrite("UpdateSeriesCatalog ERROR: " + ex.Message);
        //        }
        //    }
        //}
    }
}