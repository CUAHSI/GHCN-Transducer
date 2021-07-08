using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace CDEC_Harvester
{
    class SeriesCatalogManager
    {
        private LogWriter _log;
        private Dictionary<int, Variable> _variableLookup;

        public struct stationSeriesData
        {
            public string SensorDescription;
            public string SensorNumber;
            public string Duration;
            public string Plot;
            public string DataCollection;
            public string DataAvailable;
        }

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


        private Dictionary<int, Variable> getVariableLookup()
        {
            Dictionary<int, Variable> lookup = new Dictionary<int, Variable>();
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
                            int id = reader.GetInt32(0);
                            var variable = new Variable
                            {
                                VariableID = id,
                                VariableCode = reader.GetString(1),
                                VariableName = reader.GetString(2),
                                VariableUnitsID = reader.GetInt32(3),
                                SampleMedium = reader.GetString(4),
                                DataType = reader.GetString(5),
                                TimeSupport = Convert.ToSingle(reader.GetValue(6)),
                                TimeUnitsID = reader.GetInt32(7)
                 
                            };
                            lookup.Add(id, variable);
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
                    string sql = "SELECT MethodID, MethodDescription, MethodLink, MethodCode FROM dbo.Methods";
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
                                //MethodLink = if (reader.GetString(2) != DBNull.Value,
                                MethodCode = reader.GetString(3)
                            };
                            lookup.Add(m.MethodCode, m);
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


        public void UpdateSeriesCatalog_fast(List<Site> stations)
        {
            //Get Lookup tables
            var siteLookup = GetSiteLookup();
            var variableLookup = getVariableLookup();
            var methodLookup = getMethodLookup();
            var source = getSource();
            var varM = new VariableManager(_log);
            var unitsLookup = varM.GetODMUnits();

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString; 

            List<Series> seriesList = new List<Series>();
            
            Console.WriteLine("updating series catalog for " + seriesList.Count.ToString() + " series ...");

            //lookup sensors (variables) for each station and creat elist of staions with corresponding variabels

            foreach (var s in stations)
            {

                //Get additional data form web scraping
                var sensorsAtStation = GetseriesInfoFromWeb(s.SiteCode);

                //var sensorsAtStation = s.Sensors.Split(',');
                for (int i=0;i< sensorsAtStation.Count; i++)
                {
                    // for now only deal with daily data 
                    if (!sensorsAtStation[i].Duration.Contains("daily")) { continue; }
                    //lookup variable 
                    var v = new Variable();

                    
                    variableLookup.TryGetValue(Convert.ToInt32(sensorsAtStation[i].SensorNumber), out v);
                    if (v != null)
                    {
                        try
                        {
                            
                                //update variableinfo where possible 
                            //if (sensorsAtStation[i].Duration.Contains("month")) { v.TimeUnitsID = 106;  };
                            //if (sensorsAtStation[i].Duration.Contains("week")) { v.TimeUnitsID = 105; };
                            //if (sensorsAtStation[i].Duration.Contains("daily")) { v.TimeUnitsID = 104; };
                            //if (sensorsAtStation[i].Duration.Contains("hour")) { v.TimeUnitsID = 103; };

                            //v.ValueType = "Unknown"; //TODO : research if this can be derived fron table 
                            //if (sensorsAtStation[i].DataCollection.ToLower().Contains("manual")) { v.ValueType = "Sample"; };


                            //using (SqlConnection connection = new SqlConnection(connString))
                            //{
                            //    varM.updateVariable(v, connection);
                            //}
                            
                            var series = new Series();

                            MethodInfo m = methodLookup["0"];

                            series.VariableID = v.VariableID;
                            series.VariableCode = v.VariableCode;
                            //series.
                            series.SiteID = s.SiteID;
                            series.SiteName = s.SiteName;
                            series.SiteCode = s.SiteCode;
                            series.MethodCode = m.MethodCode;
                            series.MethodID = m.MethodID;

                            series.BeginDateTime = Convert.ToDateTime(sensorsAtStation[i].DataAvailable.Split(' ').First());
                            //endate is set to time in future as it is set to "present"
                            DateTime dt = DateTime.ParseExact("01/01/2100", "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            if (sensorsAtStation[i].DataAvailable.Split(' ').Last().Contains("present"))
                            {
                                series.EndDateTime = dt;
                            }
                            else
                            {
                                series.EndDateTime = Convert.ToDateTime(sensorsAtStation[i].DataAvailable.Split(' ').Last());
                            }

                            seriesList.Add(series);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("site: " + s.SiteCode + "error :" + ex.Message);
                        }
                    }

                }
            }

            
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
                            try
                            {
                                var variableId = seriesList[i].VariableID;
                                var siteCode = seriesList[i].SiteCode;
                                var methodCode = seriesList[i].MethodCode;
                                
                                // only include series for variables which are already in the Variables table
                                // for example "DIAG" series are excluded because the "DIAG" variable has no CUAHSI equivalent ...
                                if (variableLookup.ContainsKey(variableId) && siteLookup.ContainsKey(siteCode) )
                                {
                                    var row = bulkTable.NewRow();
                                    seriesID = seriesID + 1;
                                    Variable v = variableLookup[variableId];
                                    Site s = siteLookup[siteCode];
                                    MethodInfo m = methodLookup[methodCode];
                                    var variableUnitName = unitsLookup.First(u => u.UnitsID == v.VariableUnitsID).UnitsName;
                                    var timeUnitName = unitsLookup.First(u => u.UnitsID == v.TimeUnitsID).UnitsName;

                                    row["SeriesID"] = seriesID;
                                    row["SiteID"] = s.SiteID;
                                    row["SiteCode"] = s.SiteCode;
                                    row["SiteName"] = s.SiteName;
                                    row["SiteType"] = "Unknown";
                                    row["VariableID"] = v.VariableID;
                                    row["VariableCode"] = v.VariableCode;
                                    row["VariableName"] = v.VariableName;
                                    row["Speciation"] = v.Speciation;
                                    row["VariableUnitsID"] = v.VariableUnitsID;
                                    row["VariableUnitsName"] = variableUnitName;
                                    row["SampleMedium"] = v.SampleMedium;
                                    row["ValueType"] = v.ValueType;
                                    row["TimeSupport"] = v.TimeSupport;
                                    row["TimeUnitsID"] = v.TimeUnitsID;
                                    row["TimeUnitsName"] = timeUnitName; 
                                    row["DataType"] = v.DataType;
                                    row["GeneralCategory"] = v.GeneralCategory;
                                    row["MethodID"] = m.MethodID;
                                    row["MethodDescription"] = m.MethodDescription;
                                    row["SourceID"] = source.SourceID;
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
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
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

        public List<stationSeriesData> GetseriesInfoFromWeb(string stationId)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument doc = web.Load("https://cdec.water.ca.gov/dynamicapp/staMeta?station_id=" + stationId);

            DataTable table = new DataTable();
            //var headers = doc.DocumentNode.SelectNodes("//table[2]//tbody//tr");
            //foreach (HtmlNode header in headers)
            //    table.Columns.Add(header.InnerText); // create columns from th
            //                                         // select rows with td elements 
            var seriesList = new List<stationSeriesData>();

            //some sites have additional table e.g yub so pick second to last as all seem to have the additional commnets table which can be empty 
            var tablecount = doc.DocumentNode.SelectNodes("//table").Count;
            var tableindex = tablecount - 1;
            try
            {


                foreach (var row in doc.DocumentNode.SelectNodes("//table[" + tableindex + "]//tr"))
                {
                    var stat = new stationSeriesData();
                    var tds = row.Descendants("td");
                    for (int i = 0; i < tds.Count(); i++)
                    {
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 0)
                        {
                            stat.SensorDescription = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                        }
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 1)
                        {
                            stat.SensorNumber = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                        }
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 2)
                        {
                            stat.Duration = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart());

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart()));
                        }
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 3)
                        {
                            stat.Plot = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart());

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Replace("(", "").Replace(")", "").TrimStart()));
                        }
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 4)
                        {
                            stat.DataCollection = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml);

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml));
                        }
                        if (tds.ElementAt(i).NodeType == HtmlNodeType.Element && i == 5)
                        {
                            stat.DataAvailable = RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Trim());

                            Console.WriteLine(RemoveUnwantedTags(tds.ElementAt(i).InnerHtml.Trim()));
                        }
                    }
                    seriesList.Add(stat);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured GetseriesInfoFromWeb StationId=" + stationId + " Message: " + ex.Message);
            }
            return seriesList;
        }

        static string RemoveUnwantedTags(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            var document = new HtmlDocument();
            document.LoadHtml(data);

            var acceptableTags = new String[] { "strong", "em", "u" };

            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);

                }
            }

            return document.DocumentNode.InnerHtml;
        }


    }
}