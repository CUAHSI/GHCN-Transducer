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

namespace CoCoHarvester
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

        public List<CoCoSite> ReadSitesFromWeb()
        {
            var countries = ReadCountriesFromWeb();
            var states = ReadStatesFromWeb();
            var siteList = new List<CoCoSite>();

            string sitesUrl = "http://data.cocorahs.org/cocorahs/export/exportstations.aspx?format=csv&state=";

            Console.WriteLine("Reading Sites from URL: " + sitesUrl);
            _log.LogWrite("Reading sites from URL: " + sitesUrl);


            try
            {
                var client = new WebClient();
                using (var stream = client.OpenRead(sitesUrl))
                {
                    using (TextFieldParser csvParser = new TextFieldParser(stream))
                    {
                        csvParser.CommentTokens = new string[] { "#" };
                        csvParser.SetDelimiters(new string[] { "," });
                        csvParser.HasFieldsEnclosedInQuotes = true;

                        // Skip the row with the column names
                        csvParser.ReadLine();

                        while (!csvParser.EndOfData)
                        {
                            // Read current line fields, pointer moves to the next line.
                            string[] fields = csvParser.ReadFields();

                            CoCoSite site = new CoCoSite
                            {
                                SiteCode = fields[0],
                                SiteName = fields[1],
                                StationType = fields[2],
                                State = fields[3],
                                County = fields[4],
                                Latitude = Convert.ToSingle(fields[6]),
                                Longitude = Convert.ToSingle(fields[7]),
                                Elevation = Convert.ToSingle(fields[8]),
                                IsActive = (fields[9] == "Reporting")
                            };
                            siteList.Add(site);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _log.LogWrite("ReadSites ERROR: " + ex.Message);
            }
            return siteList;
        }


        /// <summary>
        /// Reading additional CoCoRaHS sites from the ghcnd-stations registry
        /// </summary>
        /// <returns></returns>
        public List<CoCoSite> ReadSitesFromGhcn()
        {
            var countries = ReadCountriesFromWeb();
            var states = ReadStatesFromWeb();

            string sitesUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";

            Console.WriteLine("Reading CoCoRaHS Sites from URL: " + sitesUrl);
            //_log.LogWrite("Reading sites CoCoRaHS Sites from URL: " + sitesUrl);

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

            List<CoCoSite> siteList = new List<CoCoSite>();

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

                        // networkCode == 1 means that it is a CoCoRaHS site
                        if (networkCode == "1")
                        {
                            CoCoSite site = new CoCoSite
                            {
                                SiteCode = code,
                                SiteName = name,
                                Latitude = lat,
                                Longitude = lon,
                                Elevation = elev,
                                State = stateName
                            };
                            siteList.Add(site);
                        }
                    }
                }
                Console.WriteLine(String.Format("found {0} sites", siteList.Count));
                _log.LogWrite(String.Format("found {0} sites", siteList.Count));
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSites ERROR: " + ex.Message);
            }
            return siteList;
        }






        public int ReadSeriesFromWeb(CoCoSite site)
        {
            var seriesUrl = String.Format("http://data.cocorahs.org/cocorahs/export/exportreports.aspx?ReportType=Daily&dtf=1&Format=csv&Station={0}&ReportDateType=timestamp&Date=1/1/2000&TimesInGMT=False", site.SiteCode);

            try
            {
                var client = new WebClient();

                
                using (var stream = client.OpenRead(seriesUrl))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        int n = 0;
                        string secondLine = null;
                        string lastLine = null;

                        while (!reader.EndOfStream)
                        {
                            lastLine = reader.ReadLine();
                            if (n == 1)
                            {
                                secondLine = lastLine;
                            }
                            n++;
                        }


                        // lastLine now contains the very last line from reader
                        if (secondLine == null || lastLine == null || secondLine == lastLine)
                        {
                            return 0;
                        }
                        else
                        {
                            string[] secondLineFields = secondLine.Split(',');
                            string[] lastLineFields = lastLine.Split(',');
                            site.PrecipStart = Convert.ToDateTime(secondLineFields[0]);
                            site.PrecipEnd = Convert.ToDateTime(lastLineFields[0]).AddDays(1);
                            site.Comments = site.PrecipStart.ToString("yyyy-MM-dd") + ":" + site.PrecipEnd.ToString("yyyy-MM-dd");
                            return 1;
                        }
                    }
                }
                
                /*
                var tempFile = System.IO.Path.GetTempFileName();
                client.DownloadFile(seriesUrl, tempFile);
                var firstLine = File.ReadLines(tempFile).First();
                string secondLine = File.ReadLines(tempFile).ElementAtOrDefault(1);
                var lastLine2 = File.ReadLines(tempFile).Last();
                if (secondLine == null || secondLine == lastLine2)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
                */
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSites ERROR: " + ex.Message);
                return 0;
            }
        }



        /// <summary>
        /// Updates the sites in the ODM database
        /// </summary>
        public void UpdateSites()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            List<CoCoSite> sitesList = ReadSitesFromWeb();
           

            Console.WriteLine("updating sites for " + sitesList.Count.ToString() + " sites ...");
            _log.LogWrite("UpdateSites for " + sitesList.Count.ToString() + " sites ...");

            //try
            //{
            int i = 0;
            int numActiveSites = 0;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                Dictionary<string, long> siteLookup = GetSiteLookup(connection);

                foreach (CoCoSite site in sitesList)
                {
                    i++;

                    if (i < 10000)
                    {
                        continue;
                    }

                    try
                    {
                        int siteIsActive = ReadSeriesFromWeb(site);
                        if (siteIsActive > 0)
                        {
                            bool saveResult = SaveOrUpdateSite(site, siteLookup, connection);
                            numActiveSites++;
                        }
                    }
                    catch(Exception ex)
                    {
                        _log.LogWrite("UpdateSites ERROR for site : " + site.SiteCode + " " + ex.Message);
                    }
                    if (i % 1000 == 0)
                    {
                        Console.WriteLine("SaveOrUpdateSite " + Convert.ToString(numActiveSites) + " / " + Convert.ToString(i));
                    }
                }
            }
            //}
            //catch(Exception ex)
            //{
            //    _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            //}
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
        public bool SaveOrUpdateSite(CoCoSite site, Dictionary<string, long> lookup, SqlConnection connection)
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

        public void DeleteOldSites(SqlConnection connection)
        {
            string sqlCount = "SELECT COUNT(*) FROM dbo.Sites";
            int actualSiteCount = 1000000;
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

        public void UpdateSites_fast()
        {
            var sitesList = ReadSitesFromGhcn();

            Console.WriteLine("updating sites for " + sitesList.Count.ToString() + " sites ...");
            _log.LogWrite("UpdateSites for " + sitesList.Count.ToString() + " sites ...");

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

                    // this parameter can be adjusted in case of server timeout
                    int batchSize = 500;
                    long siteID = 0L;

                    int numBatches = (sitesList.Count / batchSize) + 1;
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
                        if (batchEnd >= sitesList.Count)
                        {
                            batchEnd = sitesList.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            siteID = siteID + 1;
                            var row = bulkTable.NewRow();
                            row["SiteID"] = siteID;
                            row["SiteCode"] = sitesList[i].SiteCode;
                            row["SiteName"] = sitesList[i].SiteName;
                            row["Latitude"] = sitesList[i].Latitude;
                            row["Longitude"] = sitesList[i].Longitude;
                            row["LatLongDatumID"] = 3; // WGS1984
                            row["Elevation_m"] = sitesList[i].Elevation;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = 0.0f;
                            row["LocalY"] = 0.0f;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 0.0f;
                            row["State"] = sitesList[i].State;
                            row["County"] = sitesList[i].County;
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
                _log.LogWrite("UpdateSites: " + sitesList.Count.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}