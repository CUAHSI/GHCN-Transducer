﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CDEC_Harvester
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
                    Organization = "California Department of Water Resources",
                    SourceDescription = "The primary function of the California Data Exchange Center (CDEC) is to facilitate the collection, storage, and exchange of hydrologic and climate information to support real-time flood management and water supply needs in California.",
                    SourceLink = "https://cdec.water.ca.gov/",
                    ContactName = "",
                    Phone = " (916) 574-1777",
                    Email = "flood.webmaster@water.ca.gov",
                    Address = "1416 9th Street",
                    City = "Sacramento",
                    State = "CA",
                    ZipCode = "95814",
                    Citation = @"",
                    SourceCode = "CDEC",
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
                TopicCategory = "Unknown",
                Title = "California Data Exchange Center (CDEC)",
                Abstract = @"",
                ProfileVersion = "Unknown",
                MetadataLink = "https://cdec.water.ca.gov/queryTools.html"
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