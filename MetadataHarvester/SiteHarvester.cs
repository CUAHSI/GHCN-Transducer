using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Net;
using System.IO;
using System.Globalization;

namespace MetadataHarvester
{
    /// <summary>
    /// Auxiliary class to hold information about column
    /// positions in the ghcnd-sites file
    /// </summary>
    class SiteColumn
    {
        private int _start;
        private int _end;

        public SiteColumn(int startIndex, int endIndex)
        {
            _start = startIndex;
            _end = endIndex;
        }

        public int Start
        {
            get { return _start - 1; }
        }

        public int Length
        {
            get { return (_end - _start + 1); }
        }
    }

    class SiteHarvester
    {
        

        /// <summary>
        /// Lookup table of country codes and country names
        /// </summary>
        private Dictionary<string, string> _countries = new Dictionary<string, string>();
        /// <summary>
        /// Lookup table of state codes and state names
        /// </summary>
        private Dictionary<string, string> _states = new Dictionary<string, string>();

        /// <summary>
        /// The list of sites downloaded from GHCN site table
        /// </summary>
        private List<GhcnSite> _sites = new List<GhcnSite>();

        private Dictionary<string, SiteColumn> _siteColPos = new Dictionary<string, SiteColumn>();

        /// <summary>
        /// Initialize the SiteHarvester class
        /// </summary>
        public SiteHarvester()
        {
            // specify the positions of columns in the ghcnd-sites file
            _siteColPos.Add("code", new SiteColumn(1, 11));
            _siteColPos.Add("lat", new SiteColumn(13, 20));
            _siteColPos.Add("lon", new SiteColumn(22, 30));
            _siteColPos.Add("elevation", new SiteColumn(32, 37));
            _siteColPos.Add("state", new SiteColumn(39, 40));
            _siteColPos.Add("name", new SiteColumn(42, 71));
            _siteColPos.Add("gsnflag", new SiteColumn(73, 75));
            _siteColPos.Add("hcnflag", new SiteColumn(77, 79));
            _siteColPos.Add("wmo", new SiteColumn(81, 85));
        }

        /// <summary>
        /// Read the country lookup file from GHCN text file
        /// </summary>
        public void ReadCountries()
        {
            string countriesFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-countries.txt";

            var client = new WebClient();
            using (var stream = client.OpenRead(countriesFileUrl))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // do stuff
                    Console.WriteLine(line);
                    string code = line.Substring(0, line.IndexOf(" "));
                    string name = line.Substring(line.IndexOf(" ") + 1);
                    _countries.Add(code, name);
                }
            }

        }

        /// <summary>
        /// Read the states lookup table from GHCN text file
        /// </summary>
        public void ReadStates()
        {
            string statesFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-states.txt";

            var client = new WebClient();
            using (var stream = client.OpenRead(statesFileUrl))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // do stuff
                    Console.WriteLine(line);
                    string code = line.Substring(0, line.IndexOf(" "));
                    string name = line.Substring(line.IndexOf(" ") + 1);
                    _states.Add(code, name);
                }
            }
        }

        public void ReadStations()
        {
            Console.WriteLine("Reading Sites from GHCN file ghcn-stations.txt ...");

            string sitesUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";

            var client = new WebClient();
            using (var stream = client.OpenRead(sitesUrl))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string code = line.Substring(_siteColPos["code"].Start, _siteColPos["code"].Length);
                    string countryCode = code.Substring(0, 2);
                    string countryName = _countries[countryCode];
                    string networkCode = code.Substring(2, 1);

                    float lat = Convert.ToSingle(line.Substring(_siteColPos["lat"].Start, _siteColPos["lat"].Length), CultureInfo.InvariantCulture);
                    float lon = Convert.ToSingle(line.Substring(_siteColPos["lon"].Start, _siteColPos["lon"].Length), CultureInfo.InvariantCulture);
                    float elev = Convert.ToSingle(line.Substring(_siteColPos["elevation"].Start, _siteColPos["elevation"].Length), CultureInfo.InvariantCulture);
                    string stateCode = (line.Substring(_siteColPos["state"].Start, _siteColPos["state"].Length)).Trim();

                    string stateName = String.Empty;
                    if (!string.IsNullOrEmpty(stateCode))
                    {
                        if (_states.ContainsKey(stateCode))
                        {
                            stateName = _states[stateCode];
                        }
                    }

                    string name = line.Substring(_siteColPos["name"].Start, _siteColPos["name"].Length);
                    string gsnflag = (line.Substring(_siteColPos["gsnflag"].Start, _siteColPos["gsnflag"].Length)).Trim();
                    string hcnflag = (line.Substring(_siteColPos["hcnflag"].Start, _siteColPos["hcnflag"].Length)).Trim();
                    string wmo = String.Empty;
                    if (line.Length > _siteColPos["wmo"].Start + _siteColPos["wmo"].Length)
                    {
                        wmo = (line.Substring(_siteColPos["wmo"].Start, _siteColPos["wmo"].Length)).Trim();
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
                    _sites.Add(site);
                }
            }
            Console.WriteLine(String.Format("found {0} sites", _sites.Count));
        }

        /// <summary>
        /// Updates the sites in the ODM database
        /// </summary>
        public void UpdateSites()
        {
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                foreach (GhcnSite site in _sites)
                {
                    SaveOrUpdateSite(site, connection);
                }
            }
        }

        public void SaveOrUpdateSite(GhcnSite site, SqlConnection connection)
        {
            //string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            //using (SqlConnection connection = new SqlConnection(connString))
            //{
            object siteIdResult = null;
            using (SqlCommand cmd = new SqlCommand("SELECT SiteID FROM Sites WHERE SiteCode = @code", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode));
                connection.Open();
                siteIdResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (siteIdResult != null)
            {
                //update the site
                //site.SiteID = Convert.ToInt64(siteIdResult);
                //using (SqlCommand cmd = new SqlCommand("UPDATE Sites SET SiteName = @name, Latitude=@lat, Longitude =@lon, Elevation=@elev, County=@country, State=@state WHERE SiteCode = @code", connection))
                //{
                //    cmd.Parameters.Add(new SqlParameter("@code", site.SiteCode));
                //    cmd.Parameters.Add(new SqlParameter("@name", site.SiteName));
                //    cmd.Parameters.Add(new SqlParameter("@lat", site.Latitude));
                //    cmd.Parameters.Add(new SqlParameter("@lon", site.Longitude));
                //    cmd.Parameters.Add(new SqlParameter("@elev", site.Elevation));
                //    cmd.Parameters.Add(new SqlParameter("@country", site.Country));
                //    cmd.Parameters.Add(new SqlParameter("@state", site.State));
                //    connection.Open();
               //     cmd.ExecuteNonQuery();
               //     connection.Close();
               // }
            }
            else if (!site.CoCoRaHS & !site.Snotel)
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
            }
        }
    }
}
