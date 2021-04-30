using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace GldasHarvester
{
    /// <summary>
    /// Responsible for updating the Sources / ISOMetadata tables in ODM DB
    /// </summary>
    class SourceManager
    {
        private LogWriter _log;
        
        public SourceManager(LogWriter log)
        {
            _log = log;
        }

        public void UpdateSources()
        {
            try
            {

                int metadataID = SaveOrUpdateMetadata();
                GldasSource source = new GldasSource
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
                    MetadataID = metadataID,
                    SourceCode = "NOAH-GHCN"
                };


                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    int sourceID = SaveOrUpdateSource(source, connection);
                }
                _log.LogWrite("UpdateSources OK");

            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSources ERROR: " + ex.Message);
            }
        }


        private int SaveOrUpdateSource(GldasSource source, SqlConnection connection)
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
                //update the source
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
                                MetadataID = @metadataid,
                                SourceCode =@sourcecode
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
                    cmd.Parameters.Add(new SqlParameter("@sourcecode", source.SourceCode));
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the source
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
                                MetadataID,
                                SourceCode)
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
                                @metadataid,
                                @sourcecode)";

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
                    cmd.Parameters.Add(new SqlParameter("@sourcecode", source.SourceCode));
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


        private int SaveOrUpdateMetadata()
        {
            object metadataIDResult;
            GldasISOMetadata metadata = new GldasISOMetadata
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
    }
}