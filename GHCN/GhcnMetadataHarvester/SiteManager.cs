using System;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;

namespace GhcnHarvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        private LogWriter _log;

        // set _use_cocorahs to false if you want to exclude CoCoRaHS sites from the metadata ODM database 
        private bool _use_cocorahs = true;

        // set _use_snotel to false if you want to exclude SNOTEL sites from the metadata ODM database 
        private bool _use_snotel = true;

        public SiteManager(LogWriter log)
        {
            _log = log;

            // reading specialized configuration settings
            var settings = ConfigurationManager.GetSection("customAppSettingsGroup/customAppSettings") as NameValueCollection;

            if (settings != null)
            {
                foreach (string key in settings.AllKeys)
                {
                    if (key.ToLower() == "use_cocorahs")
                    {
                        _use_cocorahs = Convert.ToBoolean(settings[key]);
                    }
                    if (key.ToLower() == "use_snotel")
                    {
                        _use_snotel = Convert.ToBoolean(settings[key]);
                    }
                }
            }
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

        public List<GhcnSite> ReadSitesFromWeb()
        {
            var countries = ReadCountriesFromWeb();
            var states = ReadStatesFromWeb();

            string sitesUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";

            Console.WriteLine("Reading Sites from URL: " + sitesUrl);
            _log.LogWrite("Reading sites from URL: " + sitesUrl);

            // positions of columns in the ghcnd-stations file
            Dictionary<string, TextFileColumn> colPos = new Dictionary<string, TextFileColumn>();
            colPos.Add("code", new TextFileColumn(1, 11));
            colPos.Add("lat", new TextFileColumn(13, 20));
            colPos.Add("lon", new TextFileColumn(22, 30));
            colPos.Add("elevation", new TextFileColumn(32, 37));
            colPos.Add("state", new TextFileColumn(39, 40));
            colPos.Add("name", new TextFileColumn(42, 71));
            colPos.Add("gsnflag", new TextFileColumn(73, 75));
            colPos.Add("hcnflag", new TextFileColumn(77, 79));
            colPos.Add("wmo", new TextFileColumn(81, 85));
            colPos.Add("sitecode", new TextFileColumn(1, 11));
            colPos.Add("varcode", new TextFileColumn(32, 35));
            colPos.Add("firstyear", new TextFileColumn(37, 40));
            colPos.Add("lastyear", new TextFileColumn(42, 45));

            List<GhcnSite> siteList = new List<GhcnSite>();

            try
            {
                var client = new WebClient();
                using (var stream = client.OpenRead(sitesUrl))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string code = line.Substring(colPos["code"].Start, colPos["code"].Length);
                        string countryCode = code.Substring(0, 2);
                        string countryName = countries[countryCode];
                        string networkCode = code.Substring(2, 1);

                        float lat = Convert.ToSingle(line.Substring(colPos["lat"].Start, colPos["lat"].Length), CultureInfo.InvariantCulture);
                        float lon = Convert.ToSingle(line.Substring(colPos["lon"].Start, colPos["lon"].Length), CultureInfo.InvariantCulture);
                        float elev = Convert.ToSingle(line.Substring(colPos["elevation"].Start, colPos["elevation"].Length), CultureInfo.InvariantCulture);
                        string stateCode = (line.Substring(colPos["state"].Start, colPos["state"].Length)).Trim();

                        string stateName = String.Empty;
                        if (!string.IsNullOrEmpty(stateCode))
                        {
                            if (states.ContainsKey(stateCode))
                            {
                                stateName = states[stateCode];
                            }
                        }

                        string name = line.Substring(colPos["name"].Start, colPos["name"].Length);
                        string gsnflag = (line.Substring(colPos["gsnflag"].Start, colPos["gsnflag"].Length)).Trim();
                        string hcnflag = (line.Substring(colPos["hcnflag"].Start, colPos["hcnflag"].Length)).Trim();
                        string wmo = String.Empty;
                        if (line.Length > colPos["wmo"].Start + colPos["wmo"].Length)
                        {
                            wmo = (line.Substring(colPos["wmo"].Start, colPos["wmo"].Length)).Trim();
                        }
                        int? wmoID = null;
                        if (!string.IsNullOrEmpty(wmo))
                        {
                            wmoID = Convert.ToInt32(wmo);
                        }

                        GhcnSite site = new GhcnSite
                        {
                            SiteCode = code,
                            SiteName = name,
                            Latitude = lat,
                            Longitude = lon,
                            Elevation = elev,
                            WmoID = wmoID,
                            State = stateName,
                            Country = countryName,
                            NetworkFlag = networkCode,

                            GSN = (gsnflag == "GSN"),
                            HCNFlag = hcnflag,
                            CoCoRaHS = (networkCode == "1"),
                            Snotel = (networkCode == "S"),
                        };
                        siteList.Add(site);
                    }
                }
                Console.WriteLine(String.Format("found {0} sites", siteList.Count));
                _log.LogWrite(String.Format("found {0} sites", siteList.Count));
            }
            catch(Exception ex)
            {
                _log.LogWrite("ReadSites ERROR: " + ex.Message);
            }
            return siteList;
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
            List<GhcnSite> sitesList = ReadSitesFromWeb();

            List<GhcnSite> filteredSites = new List<GhcnSite>();

            // filtering out CoCoRaHS and SNOTEL sites if specified in the settings
            Console.WriteLine("use_cocorahs setting = " + _use_cocorahs.ToString());
            Console.WriteLine("use_snotel setting = " + _use_snotel.ToString());
            _log.LogWrite("use_cocorahs setting = " + _use_cocorahs.ToString());
            _log.LogWrite("use_snotel setting = " + _use_snotel.ToString());

            foreach (GhcnSite site in sitesList)
            {
                if (site.CoCoRaHS && !_use_cocorahs)
                {
                    continue;
                }
                if (site.Snotel && !_use_snotel)
                {
                    continue;
                }
                filteredSites.Add(site);
            }

            Console.WriteLine("Using " + filteredSites.Count.ToString() + " out of " + sitesList.Count.ToString() + " sites ...");
            _log.LogWrite("Using " + filteredSites.Count.ToString() + " out of " + sitesList.Count.ToString() + " sites ...");

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

                    int numBatches = (filteredSites.Count / batchSize) + 1;
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
                        if (batchEnd >= filteredSites.Count)
                        {
                            batchEnd = filteredSites.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            siteID = siteID + 1;
                            var row = bulkTable.NewRow();
                            row["SiteID"] = siteID;
                            row["SiteCode"] = filteredSites[i].SiteCode;
                            row["SiteName"] = filteredSites[i].SiteName;
                            row["Latitude"] = filteredSites[i].Latitude;
                            row["Longitude"] = filteredSites[i].Longitude;
                            row["LatLongDatumID"] = 3; // WGS1984
                            row["Elevation_m"] = filteredSites[i].Elevation;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = 0.0f;
                            row["LocalY"] = 0.0f;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 0.0f;
                            row["State"] = filteredSites[i].State;
                            row["County"] = filteredSites[i].Country;
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
                _log.LogWrite("UpdateSites: " + filteredSites.Count.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}