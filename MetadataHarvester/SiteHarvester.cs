﻿using System;
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


        public void UpdateVariables()
        {
            List<GhcnVariable> variables = new List<GhcnVariable>();
            variables.Add(new GhcnVariable
            {
                VariableCode = "SNWD",
                VariableName = "Snow depth",
                VariableUnitsID = 47,
                SampleMedium = "Snow",
                DataType = "Continuous"
            });

            variables.Add(new GhcnVariable
            {
                VariableCode = "PRCP",
                VariableName = "Precipitation",
                VariableUnitsID = 54,
                SampleMedium = "Precipitation",
                DataType = "Incremental"
            });

            variables.Add(new GhcnVariable
            {
                VariableCode = "TMAX",
                VariableName = "Temperature",
                VariableUnitsID = 96,
                SampleMedium = "Air",
                DataType = "Maximum"
            });

            variables.Add(new GhcnVariable
            {
                VariableCode = "TMIN",
                VariableName = "Temperature",
                VariableUnitsID = 96,
                SampleMedium = "Air",
                DataType = "Minimum"
            });

            variables.Add(new GhcnVariable
            {
                VariableCode = "TAVG",
                VariableName = "Temperature",
                VariableUnitsID = 96,
                SampleMedium = "Air",
                DataType = "Average"
            });

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                foreach (GhcnVariable variable in variables)
                {
                    object variableID = SaveOrUpdateVariable(variable, connection);
                    Console.WriteLine(variableID.ToString());
                }
            }
        }

        private object SaveOrUpdateVariable(GhcnVariable variable, SqlConnection connection)
        {
            //string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            //using (SqlConnection connection = new SqlConnection(connString))
            //{
            object variableIDResult = null;
            using (SqlCommand cmd = new SqlCommand("SELECT VariableID FROM Variables WHERE VariableCode = @code", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                connection.Open();
                variableIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (variableIDResult != null)
            {
                //update the variable
                variable.VariableID = Convert.ToInt32(variableIDResult);
                using (SqlCommand cmd = new SqlCommand("UPDATE Variables SET VariableName = @name, VariableUnitsID=@units, SampleMedium =@sampleMedium, DataType=@dataType, ValueType=@valueType WHERE VariableCode = @code", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                    cmd.Parameters.Add(new SqlParameter("@name", variable.VariableName));
                    cmd.Parameters.Add(new SqlParameter("@units", variable.VariableUnitsID));
                    cmd.Parameters.Add(new SqlParameter("@sampleMedium", variable.SampleMedium));
                    cmd.Parameters.Add(new SqlParameter("@dataType", variable.DataType));
                    cmd.Parameters.Add(new SqlParameter("@valueType", variable.ValueType));
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the variable
                string sql = "INSERT INTO Variables(VariableCode, VariableName, Speciation, VariableUnitsID, SampleMedium, ValueType, IsRegular, TimeSupport, TimeUnitsID, DataType, GeneralCategory, NoDataValue) VALUES (@VariableCode, @VariableName, @Speciation, @VariableUnitsID, @SampleMedium, @ValueType, @IsRegular, @TimeSupport, @TimeUnitsID, @DataType, @GeneralCategory, @NoDataValue)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@VariableCode", variable.VariableCode));
                    cmd.Parameters.Add(new SqlParameter("@VariableName", variable.VariableName));
                    cmd.Parameters.Add(new SqlParameter("@Speciation", variable.Speciation));
                    cmd.Parameters.Add(new SqlParameter("@VariableUnitsID", variable.VariableUnitsID));
                    cmd.Parameters.Add(new SqlParameter("@SampleMedium", variable.SampleMedium));
                    cmd.Parameters.Add(new SqlParameter("@ValueType", variable.ValueType));
                    cmd.Parameters.Add(new SqlParameter("@IsRegular", variable.IsRegular));
                    cmd.Parameters.Add(new SqlParameter("@TimeSupport", variable.TimeSupport));
                    cmd.Parameters.Add(new SqlParameter("@TimeUnitsID", variable.TimeUnitsID));
                    cmd.Parameters.Add(new SqlParameter("@DataType", variable.DataType));
                    cmd.Parameters.Add(new SqlParameter("@GeneralCategory", variable.GeneralCategory));
                    cmd.Parameters.Add(new SqlParameter("@NoDataValue", variable.NoDataValue));

                    // to get the inserted variable id
                    SqlParameter param = new SqlParameter("@VariableID", SqlDbType.Int);
                    param.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(param);

                    cmd.ExecuteNonQuery();
                    variableIDResult = cmd.Parameters["@VariableID"].Value;
                    connection.Close();
                }
            }
            return variableIDResult;
        }


        private int SaveOrUpdateMetadata()
        {
            object metadataIDResult;
            GhcnISOMetadata metadata = new GhcnISOMetadata
            {
                TopicCategory = "climatology/meteorology/atmosphere",
                Title = "Global Historical Climate Network - Daily (GHCN-Daily) Version 3",
                Abstract = @"The Global Historical Climatology Network - Daily (GHCN-Daily) dataset integrates daily climate observations from approximately 30 different data sources. 
Version 3 was released in September 2012 with the addition of data from two additional station networks. 
Changes to the processing system associated with the version 3 release also allowed for updates to occur 7 days a week rather than only on most weekdays. 
Version 3 contains station-based measurements from well over 90,000 land-based stations worldwide, 
about two thirds of which are for precipitation measurement only. 
Other meteorological elements include, but are not limited to, daily maximum and minimum temperature, temperature at the time of observation, snowfall and snow depth. 
Over 25,000 stations are regularly updated with observations from within roughly the last month. 
The dataset is also routinely reconstructed (usually every week) from its roughly 30 data sources to ensure that GHCN-Daily is generally in sync with its growing list of constituent sources. 
During this process, quality assurance checks are applied to the full dataset. Where possible, GHCN-Daily station data are also updated daily from a variety of data streams. 
Station values for each daily update also undergo a suite of quality checks.",
                ProfileVersion = "19115-2",
                MetadataLink = "data.nodc.noaa.gov"
            };

            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                metadataIDResult = null;
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 MetadataID FROM ISOMetadata WHERE MetadataID > 0", connection))
                {
                    connection.Open();
                    metadataIDResult = cmd.ExecuteScalar();
                    connection.Close();
                }

                if (metadataIDResult != null)
                {
                    //update the variable
                    metadata.MetadataID = Convert.ToInt32(metadataIDResult);
                    string sql = @"UPDATE dbo.ISOMetadata SET 
                                Title = @title, 
                                TopicCategory = @category, 
                                Abstract = @abstract, 
                                ProfileVersion = @profile,
                                MetadataLink = @link
                               WHERE MetadataID = @id";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@title", metadata.Title));
                        cmd.Parameters.Add(new SqlParameter("@category", metadata.TopicCategory));
                        cmd.Parameters.Add(new SqlParameter("@abstract", metadata.Abstract));
                        cmd.Parameters.Add(new SqlParameter("@profile", metadata.ProfileVersion));
                        cmd.Parameters.Add(new SqlParameter("@link", metadata.MetadataLink));
                        cmd.Parameters.Add(new SqlParameter("@id", metadata.MetadataID));
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                else
                {
                    //save the variable
                    string sql = @"INSERT INTO dbo.ISOMetadata (
                                Title,
                                TopicCategory, 
                                Abstract, 
                                ProfileVersion, 
                                MetadataLink)
                              VALUES (
                                @title,
                                @category, 
                                @abstract, 
                                @profile, 
                                @link)";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@title", metadata.Title));
                        cmd.Parameters.Add(new SqlParameter("@category", metadata.TopicCategory));
                        cmd.Parameters.Add(new SqlParameter("@abstract", metadata.Abstract));
                        cmd.Parameters.Add(new SqlParameter("@profile", metadata.ProfileVersion));
                        cmd.Parameters.Add(new SqlParameter("@link", metadata.MetadataLink));

                        // to get the inserted variable id
                        //SqlParameter param = new SqlParameter("@MetadataID", SqlDbType.Int);
                        //param.Direction = ParameterDirection.Output;
                        //cmd.Parameters.Add(param);

                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                    // to get the inserted metadata id..
                    using (SqlCommand cmd = new SqlCommand("SELECT MetadataID FROM ISOMetadata WHERE title = @title", connection))
                    {
                        connection.Open();
                        cmd.Parameters.Add(new SqlParameter("@title", metadata.Title));
                        metadataIDResult = cmd.ExecuteScalar();
                        connection.Close();
                    }
                }
            }
            return Convert.ToInt32(metadataIDResult);
        }


        public void UpdateSources()
        {
            int metadataID = SaveOrUpdateMetadata();
            GhcnSource source = new GhcnSource
            {
                Organization = "NOAA National Centers for Environmental Information",
                SourceDescription = "Global Historical Climate Network - Daily (GHCN-Daily) Version 3",
                SourceLink = "ncdc.noaa.gov",
                ContactName = "John Leslie",
                Phone = "1-828-271-4876",
                Email = "ncei.orders@noaa.gov",
                Address = "Federal Building, 151 Patton Avenue",
                City = "Asheville",
                State = "NC",
                ZipCode = "28801-5001",
                Citation = @"Cite this dataset when used as a source: 
Menne, Matthew J., Imke Durre, Bryant Korzeniewski, Shelley McNeal, Kristy Thomas, Xungang Yin, Steven Anthony, Ron Ray, Russell S. Vose, Byron E.Gleason, and Tamara G. Houston (2012): 
Global Historical Climatology Network - Daily (GHCN-Daily), Version 3. [indicate subset used]. 
NOAA National Climatic Data Center. doi:10.7289/V5D21VHZ [access date].",
                MetadataID = metadataID
            };


            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                int sourceID = SaveOrUpdateSource(source, connection);
                Console.WriteLine(sourceID.ToString());
            }
        }


        private int SaveOrUpdateSource(GhcnSource source, SqlConnection connection)
        {
            object sourceIDResult = null;
            using (SqlCommand cmd = new SqlCommand("SELECT SourceID FROM Sources WHERE Organization = @organization", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@organization", source.Organization));
                connection.Open();
                sourceIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (sourceIDResult != null)
            {
                //update the variable
                source.SourceID = Convert.ToInt32(sourceIDResult);
                string sql = @"UPDATE dbo.Sources SET 
                                SourceDescription = @desc, 
                                SourceLink = @link, 
                                ContactName = @name, 
                                Phone = @phone,
                                Email = @email,
                                Address = @address,
                                City = @city,
                                State = @state,
                                ZipCode = @zipcode,
                                Citation = @citation,
                                MetadataID = @metadataid
                               WHERE Organization = @org";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@org", source.Organization));
                    cmd.Parameters.Add(new SqlParameter("@desc", source.SourceDescription));
                    cmd.Parameters.Add(new SqlParameter("@link", source.SourceLink));
                    cmd.Parameters.Add(new SqlParameter("@name", source.ContactName));
                    cmd.Parameters.Add(new SqlParameter("@phone", source.Phone));
                    cmd.Parameters.Add(new SqlParameter("@email", source.Email));
                    cmd.Parameters.Add(new SqlParameter("@address", source.Address));
                    cmd.Parameters.Add(new SqlParameter("@city", source.City));
                    cmd.Parameters.Add(new SqlParameter("@state", source.State));
                    cmd.Parameters.Add(new SqlParameter("@zipcode", source.ZipCode));
                    cmd.Parameters.Add(new SqlParameter("@citation", source.Citation));
                    cmd.Parameters.Add(new SqlParameter("@metadataid", source.MetadataID));
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the variable
                string sql = @"INSERT INTO Sources (
                                Organization,
                                SourceDescription, 
                                SourceLink, 
                                ContactName, 
                                Phone,
                                Email,
                                Address,
                                City,
                                State,
                                ZipCode,
                                Citation,
                                MetadataID)
                              VALUES (
                                @org,
                                @desc, 
                                @link, 
                                @name, 
                                @phone,
                                @email,
                                @address,
                                @city,
                                @state,
                                @zipcode,
                                @citation,
                                @metadataid)";

                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@org", source.Organization));
                    cmd.Parameters.Add(new SqlParameter("@desc", source.SourceDescription));
                    cmd.Parameters.Add(new SqlParameter("@link", source.SourceLink));
                    cmd.Parameters.Add(new SqlParameter("@name", source.ContactName));
                    cmd.Parameters.Add(new SqlParameter("@phone", source.Phone));
                    cmd.Parameters.Add(new SqlParameter("@email", source.Email));
                    cmd.Parameters.Add(new SqlParameter("@address", source.Address));
                    cmd.Parameters.Add(new SqlParameter("@city", source.City));
                    cmd.Parameters.Add(new SqlParameter("@state", source.State));
                    cmd.Parameters.Add(new SqlParameter("@zipcode", source.ZipCode));
                    cmd.Parameters.Add(new SqlParameter("@citation", source.Citation));
                    cmd.Parameters.Add(new SqlParameter("@metadataid", source.MetadataID));
                    
                    // to get the inserted variable id
                    SqlParameter param = new SqlParameter("@SourceID", SqlDbType.Int);
                    param.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(param);

                    cmd.ExecuteNonQuery();
                    sourceIDResult = cmd.Parameters["@SourceID"].Value;
                    connection.Close();
                }

                // to get the inserted source id..
                using (SqlCommand cmd = new SqlCommand("SELECT SourceID FROM Sources WHERE Organization = @org", connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@org", source.Organization));
                    sourceIDResult = cmd.ExecuteScalar();
                    connection.Close();
                }
            }
            return Convert.ToInt32(sourceIDResult);
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
            int i = 0;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                Dictionary<string, long> siteLookup = GetSiteLookup(connection);

                foreach (GhcnSite site in _sites)
                {
                    SaveOrUpdateSite(site, siteLookup, connection);
                    i++;
                    if (i % 1000 == 0)
                    {
                        Console.WriteLine("SaveOrUpdateSite " + Convert.ToString(i));
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
                    while(r.Read())
                    {
                        lookup.Add(Convert.ToString(r["SiteCode"]), Convert.ToInt64(r["SiteID"]));
                    }
                }
                connection.Close();
            }
            return lookup;
        }

        public void SaveOrUpdateSite(GhcnSite site, Dictionary<string, long> lookup, SqlConnection connection)
        {
            if (lookup.ContainsKey(site.SiteCode))
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
