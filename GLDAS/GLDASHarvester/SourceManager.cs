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
                    Organization = "NASA / Goddard Earth Sciences Data and Information Services Center (GES DISC)",
                    SourceDescription = "GLDAS Noah Land Surface Model L4 3 hourly 0.25 x 0.25 degree V2.1",
                    SourceLink = "https://disc.gsfc.nasa.gov/datasets/GLDAS_NOAH025_3H_2.1/summary",
                    ContactName = "Hiroko Kato Beaudoing",
                    Phone = "301.254.1538",
                    Email = "207420001",
                    Address = "Goddard Earth Sciences Data and Information Services Center (GES DISC)",
                    City = "Greenbelt",
                    State = "MD",
                    ZipCode = "28801-5001",
                    Citation = @"Cite this dataset when used as a source: 
Beaudoing, H. and M. Rodell, NASA/GSFC/HSL (2020), GLDAS Noah Land Surface Model L4 3 hourly 0.25 x 0.25 degree V2.1, 
Greenbelt, Maryland, USA, Goddard Earth Sciences Data and Information Services Center (GES DISC), 
Accessed: [Data Access Date]",
                    MetadataID = metadataID,
                    SourceCode = "GES-DISC"
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
                Title = "GLDAS Noah Land Surface Model L4 3 hourly 0.25 x 0.25 degree V2.1",
                Abstract = @"NASA Global Land Data Assimilation System Version 2 (GLDAS-2) has three components: 
GLDAS-2.0, GLDAS-2.1, and GLDAS-2.2. GLDAS-2.0 is forced entirely with the Princeton meteorological forcing input data and provides a temporally consistent series from 1948 through 2014. GLDAS-2.1 is forced with a combination of model and observation data from 2000 to present. 
GLDAS-2.2 product suites use data assimilation (DA), whereas the GLDAS-2.0 and GLDAS-2.1 products are open - loop (i.e., no data assimilation). 
The choice of forcing data, as well as DA observation source, variable, and scheme, vary for different GLDAS-2.2 products.
GLDAS - 2.1 data products are now available in two production streams: one stream is forced with combined forcing data including GPCP version 1.3
(the main production stream), and the other stream is processed without this forcing data(the early production stream). 
Since the GPCP Version 1.3 data have a 3 - 4 month latency, the GLDAS - 2.1 data products are first created without it, 
and are designated as Early Products(EPs), with about 1.5 month latency. 
Once the GPCP Version 1.3 data become available, the GLDAS-2.1 data products are processed in the main production stream and are removed from the Early Products archive.
This data product, reprocessed in January 2020, is for GLDAS - 2.1 Noah 3 - hourly 0.25 degree data from the main production stream and it is a replacement to its previous version.
The 3 - hourly data product was simulated with the Noah Model 3.6 in Land Information System(LIS) Version 7.The data product contains 36 land surface fields from January 2000 to present.
The GLDAS - 2.1 data are archived and distributed in NetCDF format.The GLDAS - 2.1 products supersede their corresponding GLDAS - 1 products.",
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