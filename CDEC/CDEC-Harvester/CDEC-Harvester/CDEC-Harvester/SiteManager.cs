using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CDEC_Harvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        private LogWriter _log;

        public SiteManager(LogWriter log)
        {
            _log = log;
        }
        public List<string> getStationList()
        {
            var stations = new List<string>();
            string url = "https://cdec.water.ca.gov/dynamicapp/staMeta?station_id=";

            try
            {
                HtmlWeb web = new HtmlWeb();

                HtmlDocument doc = web.Load(url);

                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    HtmlAttribute att = link.Attributes["href"];
                    if (att.Value.Contains("dynamicapp/staMeta?station_id"))
                    {
                        // showing output
                        //Console.WriteLine(att.Value);

                        stations.Add(att.Value.Split('=')[1]);
                    }
                }
            }
            catch (WebException ex)
            {
                throw ex;
            }

            return stations;
        }


        public List<Site> getMetadatafromStationPage(List<string> stationIds)
        {
            var sites = new List<Site>();
            string url = "https://cdec.water.ca.gov/dynamicapp/staMeta?station_id=";

            try
            {
                foreach (var item in stationIds)
                {
                    HtmlWeb web = new HtmlWeb();

                    HtmlDocument doc = web.Load(url + item);

                    var node = doc.DocumentNode.SelectNodes("//table[1]/tr/td");

                    if (node == null)
                    {
                        Console.WriteLine ( "error SelectNodes for stationId: " + item );
                        continue;
                    }
                    var site = new Site();
                    for (var i = 0; i < node.Count - 1; i++)
                    {
                        var name = doc.DocumentNode.SelectSingleNode("//h2").InnerHtml;
                        site.SiteName = name;

                        if (node[i].NodeType == HtmlNodeType.Element && i % 2 == 0)
                        {
                            if (node[i].InnerHtml.ToLower().Contains("station id")) { site.SiteCode = (node[i+1].InnerHtml);}
                            if (node[i].InnerHtml.ToLower().Contains("latitude")) 
                            {
                                var cleanedString = (node[i + 1].InnerHtml.Replace("&#176","")); //some cordinates contain the degree and that is the htmlencoded
                                site.Latitude = Convert.ToDecimal(cleanedString); 
                            }
                            if (node[i].InnerHtml.ToLower().Contains("longitude")) 
                            {
                                var cleanedString = (node[i + 1].InnerHtml.Replace("&#176", "")); //some cordinates contain the degree and that is the htmlencoded
                                site.Longitude = Convert.ToDecimal(cleanedString); 
                            }
                            if (node[i].InnerHtml.ToLower().Contains("county")) { site.County = node[i + 1].InnerHtml; }
                            if (node[i].InnerHtml.ToLower().Contains("elevation")) 
                            {
                                var cleanedString = Regex.Replace(node[i + 1].InnerHtml, @"[^0-9]+","");
                                site.Elevation = Convert.ToDecimal(cleanedString); 
                            }
                            
                        }
                    }
                    sites.Add(site);
                }
            }
            catch (WebException ex)
            {
                throw ex;
            }

            return sites;
        }

        public List<StationInfo> getMetadataForStation(List<string> stations)
        {
            string url = "https://cdec.water.ca.gov/dynamicapp/wsSensorData/getSensorsForStation?&stationId=";

            var stationList = new List<StationInfo>();

            foreach (var s in stations)
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = url;
                    var jsonData = webClient.DownloadString(url + s);
                    var item = JsonConvert.DeserializeObject<StationInfo>(jsonData);
                    stationList.Add(item);

                    _log.LogWrite(String.Format("Processing: ", s));
                    }
            }
            catch (WebException ex)
            {
                throw ex; //bubble up exception
            }
            return stationList;
        }
        public void DeleteOldSites(int siteCount, SqlConnection connection)
        {
            string sqlCount = "SELECT COUNT(*) FROM dbo.Sites";
            int actualSiteCount = siteCount;
            using (var cmd = new SqlCommand(sqlCount, connection))
            {
                try
                {
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    actualSiteCount = Convert.ToInt32(result);
                    Console.WriteLine("number of old sites to delete " + result.ToString());
                }
                catch (Exception ex)
                {
                    var msg = "finding site count " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }

            string sqlDelete = "DELETE FROM dbo.Sites WHERE SiteID < @id";

            // it is necessary to delete old sites with smaller batches because
            // deleting all sites in one command results in transaction log exception
            int i = 0;
            int batchSize = 500;
            while (i <= actualSiteCount)
            {
                using (SqlCommand cmd = new SqlCommand(sqlDelete, connection))
                {
                    i = i + batchSize;
                    try
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@id", i));
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("deleting old sites ... " + i.ToString());
                    }
                    catch (Exception ex)
                    {
                        var msg = "error deleting old sites " + i.ToString() + " " + ex.Message;
                        Console.WriteLine(msg);
                        _log.LogWrite(msg);
                        return;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            string sqlReset = @"DBCC CHECKIDENT('dbo.Sites', RESEED, 1);";
            using (SqlCommand cmd = new SqlCommand(sqlReset, connection))
            {
                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("reset id of Sites Table");

                }
                catch (Exception ex)
                {
                    var msg = "error deleting old Sites table: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void UpdateSites_fast(List<Site> sites)
        {
            //var siteList = new List<StationsModel>();


            try
            {

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    // delete old entries from series catalog (necessary for deleting entries from Sites table)
                    string sqlDeleteSeries = "TRUNCATE TABLE dbo.SeriesCatalog";
                    using (SqlCommand cmd = new SqlCommand(sqlDeleteSeries, connection))
                    {
                        try
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            _log.LogWrite("deleted old series from SeriesCatalog");
                        }
                        catch (Exception ex)
                        {
                            _log.LogWrite("error deleting old SeriesCatalog table: " + ex.Message);
                            return;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                    // delete old entries from "sites" table
                    // using batch delete 
                    DeleteOldSites(sites.Count, connection);
                    

                    // to be adjusted
                    int batchSize = 500;
                    long siteID = 0L;

                    int numBatches = (sites.Count / batchSize) + 1;
                    for (int b = 0; b < numBatches; b++)
                    {
                        // prepare for bulk insert
                        DataTable bulkTable = new DataTable();

                        bulkTable.Columns.Add("SiteID", typeof(long));
                        bulkTable.Columns.Add("SiteCode", typeof(string));
                        bulkTable.Columns.Add("SiteName", typeof(string));
                        bulkTable.Columns.Add("Latitude", typeof(float));
                        bulkTable.Columns.Add("Longitude", typeof(float));
                        bulkTable.Columns.Add("LatLongDatumID", typeof(int));
                        bulkTable.Columns.Add("Elevation_m", typeof(float));
                        bulkTable.Columns.Add("VerticalDatum", typeof(string));
                        bulkTable.Columns.Add("LocalX", typeof(float));
                        bulkTable.Columns.Add("LocalY", typeof(float));
                        bulkTable.Columns.Add("LocalProjectionID", typeof(int));
                        bulkTable.Columns.Add("PosAccuracy_m", typeof(float));
                        bulkTable.Columns.Add("State", typeof(string));
                        bulkTable.Columns.Add("County", typeof(string));
                        bulkTable.Columns.Add("Comments", typeof(string));
                        bulkTable.Columns.Add("SiteType", typeof(string));

                        int batchStart = b * batchSize;
                        int batchEnd = batchStart + batchSize;
                        if (batchEnd >= sites.Count)
                        {
                            batchEnd = sites.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            siteID = siteID + 1;
                            var row = bulkTable.NewRow();
                            row["SiteID"] = siteID;
                            row["SiteCode"] = sites[i].SiteCode;
                            row["SiteName"] = sites[i].SiteName;
                            row["Latitude"] = sites[i].Latitude;
                            row["Longitude"] = sites[i].Longitude;
                            row["LatLongDatumID"] = 3; // WGS1984
                            row["Elevation_m"] = sites[i].Elevation;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = 0.0f;
                            row["LocalY"] = 0.0f;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 0.0f;
                            row["State"] = "Unknown";
                            row["County"] = sites[i].County;
                            row["Comments"] = String.Empty;
                            row["SiteType"] = "Unknown"; ; // from CUAHSI SiteTypeCV controlled vocabulary
                            bulkTable.Rows.Add(row);
                        }
                        SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
                        bulkCopy.DestinationTableName = "dbo.Sites";
                        connection.Open();
                        bulkCopy.WriteToServer(bulkTable);
                        connection.Close();
                        Console.WriteLine("Sites inserted row " + batchEnd.ToString());
                    }
                }
                _log.LogWrite("UpdateSites: " + sites.Count.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}