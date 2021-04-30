using System;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;

namespace GldasHarvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        private LogWriter _log;
        private string _sites_url;

        public SiteManager(LogWriter log)
        {
            _log = log;

            // reading specialized configuration settings
            var settings = ConfigurationManager.GetSection("customAppSettingsGroup/customAppSettings") as NameValueCollection;

            if (settings != null)
            {
                foreach (string key in settings.AllKeys)
                {
                    if (key.ToLower() == "sites_url")
                    {
                        _sites_url = Convert.ToString(settings[key]);
                    }
                }
            }
        }


        public List<GldasSite> ReadSites()
        {
            string sitesUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";

            // During "build solution" the CSV file is moved to bin/Debug or bin/Release
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string sitesFile = Path.Combine(executableLocation, "settings", "gldas_points_025d.csv");

            Console.WriteLine("Reading Sites from file: " + sitesFile);

            List<GldasSite> sites = new List<GldasSite>();

            try
            {
                using (var reader = new StreamReader(sitesFile))
                {
                    List<string> listA = new List<string>();
                    List<string> listB = new List<string>();
                    reader.ReadLine();
                    var siteId = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        siteId += 1;
                        var longitude = Convert.ToSingle(values[1], CultureInfo.InvariantCulture);
                        var latitude = Convert.ToSingle(values[2], CultureInfo.InvariantCulture);
                        GldasSite site = new GldasSite(siteId, latitude, longitude);
                        sites.Add(site);
                    }
                }
                Console.WriteLine(String.Format("found {0} sites", sites.Count));
                _log.LogWrite(String.Format("found {0} sites", sites.Count));
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSites ERROR: " + ex.Message);
            }
            return sites;
        }

        
        /// <summary>
        /// delete content of Sites table before a new update
        /// </summary>
        /// <param name="connection"></param>
        public void DeleteOldSites(SqlConnection connection)
        {
            // find the actual site count from database table
            string sqlCount = "SELECT COUNT(*), MIN(SiteID), MAX(SiteID) FROM dbo.Sites";
            int actualSiteCount = 1000000;
            int minSiteID = 0;
            int maxSiteID = actualSiteCount;
            using (var cmd = new SqlCommand(sqlCount, connection))
            {
                try
                {
                    connection.Open();

                    var reader = cmd.ExecuteReader();
                    reader.Read();
                    actualSiteCount = reader.GetInt32(0);
                    minSiteID = reader.GetInt32(1);
                    maxSiteID = reader.GetInt32(2);

                    Console.WriteLine("number of old sites to delete " + actualSiteCount.ToString());
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

            int i = minSiteID;
            int batchSize = 500;
            while (i <= maxSiteID)
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
            string sqlReset = @"DBCC CHECKIDENT('dbo.Sites', RESEED, 0);";
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

        /// <summary>
        /// Update ODM Sites table using bulk insert method
        /// </summary>
        public void UpdateSites_fast()
        {
            List<GldasSite> sites = ReadSites();

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
                            Console.WriteLine("deleted old series from SeriesCatalog");
                            _log.LogWrite("deleted old series from SeriesCatalog");
                        }
                        catch (Exception ex)
                        {
                            var msg = "error deleting old SeriesCatalog table: " + ex.Message;
                            Console.WriteLine(msg);
                            _log.LogWrite(msg);
                            return;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                    // delete old entries from "sites" table using batch delete 
                    DeleteOldSites(connection);
                    

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
                            row["Elevation_m"] = 0.0;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = 0.0f;
                            row["LocalY"] = 0.0f;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 0.0f;
                            row["State"] = "unknown";
                            row["County"] = "unknown";
                            row["Comments"] = "no comment";
                            row["SiteType"] = "Atmosphere";
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