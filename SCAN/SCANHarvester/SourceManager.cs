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
                    Organization = "Community Collaborative Rain, Hail and Snow Network",
                    SourceDescription = "Community Collaborative Rain, Hail and Snow Network",
                    SourceLink = "www.cocorahs.org",
                    ContactName = "CoCoRaHS",
                    Phone = "",
                    Email = "info@cocorahs.org",
                    Address = "",
                    City = "",
                    State = "",
                    ZipCode = "",
                    Citation = @"Community Collaborative Rain, Hail and Snow Network (www.cocorahs.org)",
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


        private int SaveOrUpdateMetadata()
        {
            object metadataIDResult;
            var metadata = new ISOMetadata
            {
                TopicCategory = "climatology/meteorology/atmosphere",
                Title = "Community Collaborative Rain, Hail and Snow Network",
                Abstract = @"CoCoRaHS is a unique, non-profit, community-based network of volunteers 
of all ages and backgrounds working together to measure and map 
precipitation (rain, hail and snow). By using low-cost measurement tools, 
stressing training and education, and utilizing an interactive 
Web-site, our aim is to provide the highest quality data for natural 
resource, education and research applications.",
                ProfileVersion = "19115-2",
                MetadataLink = "www.cocorahs.org"
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