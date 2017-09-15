using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SCANHarvester
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
                Source source = new Source
                {
                    Organization = "United States Department of Agriculture Natural Resources Conservation Service",
                    SourceDescription = "Soil Climate Analysis Network",
                    SourceLink = "www.wcc.nrcs.usda.gov/about/mon_scan.html",
                    ContactName = "Deb Harms",
                    Phone = "503-414-3050",
                    Email = "deb.harms@por.usda.gov",
                    Address = "",
                    City = "",
                    State = "",
                    ZipCode = "",
                    Citation = @"USDA NRCS National Water and Climate Center Soil Climate Analysis Network (SCAN) ",
                    SourceCode = "NRCS-WCC-SCAN",
                    MetadataID = metadataID
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


        private int SaveOrUpdateSource(Source source, SqlConnection connection)
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
                                SourceCode = @sourcecode
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
            var metadata = new ISOMetadata
            {
                TopicCategory = "climatology/meteorology/atmosphere",
                Title = "Soil Climate Analysis Network",
                Abstract = @"As part of the Snow Survey and Water Supply Forecasting (SSWSF) Program, 
the National Water and Climate Center administers a soil climate monitoring program consisting of 
automated data collection sites across the U.S. The Soil Climate Analysis Network (SCAN) began as 
a soil moisture/soil temperature pilot project of the Natural Resources Conservation Service in 1991. 
The system is designed to provide data to support natural resource assessments and conservation activities. 
The SCAN system focuses on agricultural areas of the U.S. and is composed of over 200 stations. 
A typical SCAN site monitors soil moisture content at several depths, air temperature, relative humidity, solar radiation, 
wind speed and direction, liquid precipitation, and barometric pressure.",
                ProfileVersion = "19115-2",
                MetadataLink = "catalog.data.gov/dataset/soil-climate-analysis-network-scan"
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