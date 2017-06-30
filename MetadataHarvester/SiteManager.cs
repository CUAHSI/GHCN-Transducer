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

namespace MetadataHarvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        /// <summary>
        /// Read the lookup of countries (country code -- country name)
        /// </summary>
        /// <returns>A dictionary (country code -> country name)</returns>
        public Dictionary<string, string> ReadCountriesFromWeb()
        {
            var countries = new Dictionary<string, string>();
            string countriesFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-countries.txt";

            LogWriter.LogWrite("ReadCountries from URL " + countriesFileUrl);

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
                LogWriter.LogWrite(String.Format("Found {0} countries.", countries.Count));
            }
            catch(Exception ex)
            {
                LogWriter.LogWrite("ReadCountries ERROR: " + ex.Message);
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

            LogWriter.LogWrite("Read States from URL " + statesFileUrl);

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
                    LogWriter.LogWrite(String.Format("Found {0} states.", states.Count));
                }
            }
            catch(Exception ex)
            {
                LogWriter.LogWrite("ReadStates ERROR: " + ex.Message);
            }
            return states;
        }

        public List<GhcnSite> ReadSitesFromWeb()
        {
            var countries = ReadCountriesFromWeb();
            var states = ReadStatesFromWeb();

            string sitesUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";

            Console.WriteLine("Reading Sites from URL: " + sitesUrl);
            LogWriter.LogWrite("Reading sites from URL: " + sitesUrl);

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
                LogWriter.LogWrite(String.Format("found {0} sites", siteList.Count));
            }
            catch(Exception ex)
            {
                LogWriter.LogWrite("ReadSites ERROR: " + ex.Message);
            }
            return siteList;
        }



        /// <summary>
        /// Updates the sites in the ODM database
        /// </summary>
        public void UpdateSites()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            List<GhcnSite> sitesList = ReadSitesFromWeb();

            Console.WriteLine("updating sites for " + sitesList.Count.ToString() + " sites ...");
            LogWriter.LogWrite("UpdateSites for " + sitesList.Count.ToString() + " sites ...");

            try
            {

                int i = 0;
                int added = 0;
                int updated = 0;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    Dictionary<string, long> siteLookup = GetSiteLookup(connection);

                    foreach (GhcnSite site in sitesList)
                    {
                        if (!site.CoCoRaHS && !site.Snotel)
                        {
                            bool insertUpdateResult = SaveOrUpdateSite(site, siteLookup, connection);
                            if (insertUpdateResult)
                            {
                                added++;
                            }
                            else
                            {
                                updated++;
                            }
                            i++;
                            if (i % 1000 == 0)
                            {
                                Console.WriteLine("SaveOrUpdateSite " + Convert.ToString(i));
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                LogWriter.LogWrite("UpdateSites ERROR: " + ex.Message);
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
        public bool SaveOrUpdateSite(GhcnSite site, Dictionary<string, long> lookup, SqlConnection connection)
        {
            if (lookup.ContainsKey(site.SiteCode))
            {
                //update the site
                site.SiteID = lookup[site.SiteCode];
                using (SqlCommand cmd = new SqlCommand("UPDATE Sites SET SiteName = @name, Latitude=@lat, Longitude =@lon, Elevation_m=@elev, County=@country, State=@state WHERE SiteID = @id", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", site.SiteID));
                    cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                    cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                    cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                    cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                    cmd.Parameters.Add(new SqlParameter("@country", site.Country));
                    cmd.Parameters.Add(new SqlParameter("@state", site.State));
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                return false;
            }
            else
            {
                //save the site
                using (SqlCommand cmd = new SqlCommand("INSERT INTO Sites(SiteCode, SiteName, Latitude, Longitude, Elevation_m, County, State, SiteType, VerticalDatum, LatLongDatumID) VALUES (@code, @name, @lat, @lon, @elev, @country, @state, @siteType, @verticalDatum, @latLongDatumID)", connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode));
                    cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                    cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                    cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                    cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                    cmd.Parameters.Add(new SqlParameter("@country", site.Country));
                    cmd.Parameters.Add(new SqlParameter("@state", site.State));
                    cmd.Parameters.Add(new SqlParameter("@siteType", "Atmosphere"));
                    cmd.Parameters.Add(new SqlParameter("@verticalDatum", "Unknown"));
                    cmd.Parameters.Add(new SqlParameter("@latLongDatumID", 3));
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                return true;
            }
        }


        public void UpdateSites_fast()
        {
            List<GhcnSite> sitesList = ReadSitesFromWeb();

            Console.WriteLine("updating sites for " + sitesList.Count.ToString() + " sites ...");
            LogWriter.LogWrite("UpdateSites for " + sitesList.Count.ToString() + " sites ...");

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
                            LogWriter.LogWrite("deleted old series from SeriesCatalog");
                        }
                        catch (Exception ex)
                        {
                            var msg = "error deleting old SeriesCatalog table: " + ex.Message;
                            Console.WriteLine(msg);
                            LogWriter.LogWrite(msg);
                            return;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                    // delete old entries from "sites" table
                    string sqlDeleteSites = "TRUNCATE TABLE dbo.Sites";
                    using (SqlCommand cmd = new SqlCommand(sqlDeleteSites, connection))
                    {
                        try
                        {
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            Console.WriteLine("deleted old sites from Sites table");
                        }
                        catch (Exception ex)
                        {
                            var msg = "error deleting old sites from Sites table: " + ex.Message;
                            Console.WriteLine(msg);
                            LogWriter.LogWrite(msg);
                            return;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                    // to be adjusted
                    int batchSize = 500;

                    int numBatches = (sitesList.Count / batchSize) + 1;
                    for (int b = 0; b < numBatches; b++)
                    {
                        // prepare for bulk insert
                        DataTable bulkTable = new DataTable();

                        bulkTable.Columns.Add("SiteCode", typeof(string));
                        bulkTable.Columns.Add("SiteName", typeof(string));
                        bulkTable.Columns.Add("Latitude", typeof(double));
                        bulkTable.Columns.Add("Longitude", typeof(double));
                        bulkTable.Columns.Add("Elevation_m", typeof(double));
                        bulkTable.Columns.Add("County", typeof(string));
                        bulkTable.Columns.Add("State", typeof(string));
                        bulkTable.Columns.Add("SiteType", typeof(string));
                        bulkTable.Columns.Add("VerticalDatum", typeof(string));
                        bulkTable.Columns.Add("LatLongDatumID", typeof(int));

                        int batchStart = b * batchSize;
                        int batchEnd = batchStart + batchSize;
                        if (batchEnd >= sitesList.Count)
                        {
                            batchEnd = sitesList.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            var row = bulkTable.NewRow();
                            row["SiteCode"] = sitesList[i].SiteCode;
                            row["SiteName"] = sitesList[i].SiteName;
                            row["Latitude"] = sitesList[i].Latitude;
                            row["Longitude"] = sitesList[i].Longitude;
                            row["Elevation_m"] = sitesList[i].Elevation;
                            row["State"] = sitesList[i].State;
                            row["SiteType"] = "Atmosphere";
                            row["VerticalDatum"] = "Unknown";
                            row["LatLongDatumID"] = 3;
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
                LogWriter.LogWrite("UpdateSites: " + sitesList.Count.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                LogWriter.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}
