using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace SNOTELHarvester
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
        
        /// <summary>
        /// Read the lookup of countries (country code -- country name)
        /// </summary>
        /// <returns>A dictionary (country code -> country name)</returns>
        public Dictionary<string, string> ReadCountriesFromWeb()
        {
            var countries = new Dictionary<string, string>();
            string countriesFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-countries.txt";

            _log.LogWrite("ReadCountries from URL " + countriesFileUrl);

            try
            {
                var client = new WebClient();
                using (var stream = client.OpenRead(countriesFileUrl))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string code = line.Substring(0, line.IndexOf(" "));
                        string name = line.Substring(line.IndexOf(" ") + 1);
                        countries.Add(code, name);
                    }
                }
                _log.LogWrite(String.Format("Found {0} countries.", countries.Count));
            }
            catch(Exception ex)
            {
                _log.LogWrite("ReadCountries ERROR: " + ex.Message);
            }
            return countries;
        }

        /// <summary>
        /// Read the states lookup table from GHCN text file
        /// </summary>
        public Dictionary<string, string> ReadStatesFromWeb()
        {
            var states = new Dictionary<string, string>();
            string statesFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-states.txt";

            _log.LogWrite("Read States from URL " + statesFileUrl);

            try
            {

                var client = new WebClient();
                using (var stream = client.OpenRead(statesFileUrl))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string code = line.Substring(0, line.IndexOf(" "));
                        string name = line.Substring(line.IndexOf(" ") + 1);
                        states.Add(code, name);
                    }
                    _log.LogWrite(String.Format("Found {0} states.", states.Count));
                }
            }
            catch(Exception ex)
            {
                _log.LogWrite("ReadStates ERROR: " + ex.Message);
            }
            return states;
        }
        
        /// <summary>
        /// Updates the sites in the ODM database
        /// </summary>
        public void UpdateSites_old()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            
            var client = new AwdbClient(_log);
            List<Site> sitesList = client.GetStations();
           
            _log.LogWrite("UpdateSites for " + sitesList.Count.ToString() + " sites ...");

            int i = 0;
            int numActiveSites = 0;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                Dictionary<string, long> siteLookup = GetSiteLookup(connection);

                foreach (Site site in sitesList)
                {
                    i++;

                    try
                    {
                        bool saveResult = SaveOrUpdateSite(site, siteLookup, connection);
                    }
                    catch(Exception ex)
                    {
                        _log.LogWrite("UpdateSites ERROR for site : " + site.SiteCode + " " + ex.Message);
                    }
                    if (i % 1000 == 0)
                    {
                        _log.LogWrite("SaveOrUpdateSite " + Convert.ToString(numActiveSites) + " / " + Convert.ToString(i));
                    }
                }
            }
        }

        private Dictionary<string, long> GetSiteLookup(SqlConnection connection)
        {
            Dictionary<string, long> lookup = new Dictionary<string, long>();
            using (SqlCommand cmd = new SqlCommand("SELECT SiteCode, SiteID FROM dbo.Sites", connection))
            {
                connection.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        lookup.Add(Convert.ToString(r["SiteCode"]), Convert.ToInt64(r["SiteID"]));
                    }
                }
                connection.Close();
            }
            return lookup;
        }

        /// <summary>
        /// Saves or updates a site in the Sites database table
        /// </summary>
        /// <param name="site">the site to insert or update</param>
        /// <param name="lookup">lookup table</param>
        /// <param name="connection">the db connection</param>
        /// <returns>true if site is added, false if site is updated</returns>
        public bool SaveOrUpdateSite(Site site, Dictionary<string, long> lookup, SqlConnection connection)
        {
            if (lookup.ContainsKey(site.SiteCode))
            {
                //update the site
                site.SiteID = lookup[site.SiteCode];
                using (SqlCommand cmd = new SqlCommand("UPDATE Sites SET SiteName = @name, Latitude=@lat, Longitude =@lon, Elevation_m=@elev, County=@country, State=@state, Comments=@comments WHERE SiteID = @id", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", site.SiteID));
                    cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                    cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                    cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                    cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                    cmd.Parameters.Add(new SqlParameter("@country", site.County));
                    cmd.Parameters.Add(new SqlParameter("@state", site.State));
                    cmd.Parameters.Add(new SqlParameter("@comments", site.Comments));
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                return false;
            }
            else
            {
                //save the site
                using (SqlCommand cmd = new SqlCommand("INSERT INTO Sites(SiteCode, SiteName, Latitude, Longitude, Elevation_m, County, State, SiteType, VerticalDatum, LatLongDatumID, Comments) VALUES (@code, @name, @lat, @lon, @elev, @country, @state, @siteType, @verticalDatum, @latLongDatumID, @comments)", connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode));
                    cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                    cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                    cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                    cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                    cmd.Parameters.Add(new SqlParameter("@country", site.County));
                    cmd.Parameters.Add(new SqlParameter("@state", site.State));
                    cmd.Parameters.Add(new SqlParameter("@siteType", "Atmosphere"));
                    cmd.Parameters.Add(new SqlParameter("@verticalDatum", "Unknown"));
                    cmd.Parameters.Add(new SqlParameter("@latLongDatumID", 3));
                    cmd.Parameters.Add(new SqlParameter("@comments", site.Comments));
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                return true;
            }
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

        public void UpdateSites_fast()
        {
            var client = new AwdbClient(_log);
            List<Site> siteList = client.GetStations();

            //string[] uniqueVariables = client.ListUniqueVariables();
            //string[] uniqueElements = client.ListUniqueElements();
            //Array.Sort(uniqueElements);

            _log.LogWrite("UpdateSites for " + siteList.Count.ToString() + " sites ...");

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
                    DeleteOldSites(siteList.Count, connection);
                    

                    // to be adjusted
                    int batchSize = 500;
                    long siteID = 0L;

                    int numBatches = (siteList.Count / batchSize) + 1;
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
                        if (batchEnd >= siteList.Count)
                        {
                            batchEnd = siteList.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            siteID = siteID + 1;
                            var row = bulkTable.NewRow();
                            row["SiteID"] = siteID;
                            row["SiteCode"] = siteList[i].SiteCode;
                            row["SiteName"] = siteList[i].SiteName;
                            row["Latitude"] = siteList[i].Latitude;
                            row["Longitude"] = siteList[i].Longitude;
                            row["LatLongDatumID"] = 3; // WGS1984
                            row["Elevation_m"] = siteList[i].Elevation;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = 0.0f;
                            row["LocalY"] = 0.0f;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 0.0f;
                            row["State"] = siteList[i].State;
                            row["County"] = siteList[i].County;
                            row["Comments"] = siteList[i].Comments;
                            row["SiteType"] = "Soil hole"; // from CUAHSI SiteTypeCV controlled vocabulary
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
                _log.LogWrite("UpdateSites: " + siteList.Count.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}