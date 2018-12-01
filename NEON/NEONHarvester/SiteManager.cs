using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace NEONHarvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        private LogWriter _log;
        private NeonApiReader _apiReader;
        private List<string> _sensorPositionFileNames;

        public SiteManager(LogWriter log)
        {
            _log = log;
            _apiReader = new NeonApiReader(log);
            _sensorPositionFileNames = new List<string>();
        }

        /// <summary>
        /// Deletes all sites from ODM Sites table before inserting new sites.
        /// </summary>
        /// <param name="siteCount">Total number of sites to delete</param>
        /// <param name="connection">database connection</param>        
        public void DeleteOldSites(int siteCount, SqlConnection connection)
        {
            string sqlCount = "SELECT COUNT(*) FROM dbo.Sites";
            int actualSiteCount = siteCount;
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
            string sqlReset = @"DBCC CHECKIDENT('dbo.Sites', RESEED, 1);";
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
        /// Retrieves information about all available NEON sites from the NEON API.
        /// </summary>
        /// <returns>Returns a dictionary [NEON Site Code] --> [NeonSite object]</returns>
        public Dictionary<string, NeonSite> GetNeonSites()
        {
            _log.LogWrite("Retrieving NEON sites from API..");
            var siteInfoList = _apiReader.ReadSitesFromApi().data;
            Dictionary<string, NeonSite> neonSiteCodeLookup = new Dictionary<string, NeonSite>();
            foreach (NeonSite neonSite in siteInfoList)
            {
                neonSiteCodeLookup.Add(neonSite.siteCode, neonSite);
            }
            return neonSiteCodeLookup;
        }

        /// <summary>
        /// Retrieves NEON sensor positions and generates CUAHSI site codes.
        /// Each CUAHSI site code corresponds to one unique NEON sensor position
        /// </summary>
        /// <param name="siteSeriesLookup">A lookup NEONSiteCode -> NEONSite</param>
        /// <returns>A dictionary CUAHSI Site Code -> NEONSensorPosition</returns>
        public CuahsiSiteNeonSensorLookup GetSensorPositions(Dictionary<string, NeonSite> siteSeriesLookup)
        {
            _log.LogWrite("Retrieving sensor positions from NEON API...");

            var cuahsiSiteNeonSensorLookup = new CuahsiSiteNeonSensorLookup();
            var sensorSiteLookup = cuahsiSiteNeonSensorLookup.Lookup;

            var senPosReader = new SensorPositionReader(_log);

            List<string> supportedProductCodes = new List<string>();
            var lookupReader = new LookupFileReader(_log);
            supportedProductCodes = lookupReader.ReadProductCodesFromExcel();

            int numNeonSites = siteSeriesLookup.Keys.Count;
            int numProcessed = 0;
            foreach(string neonSiteCode in siteSeriesLookup.Keys)
            {
                numProcessed += 1;

                NeonSite neonSite = siteSeriesLookup[neonSiteCode];

                _log.LogWrite(String.Format("processing NEON site {0} ({1}/{2})", neonSiteCode, numProcessed, numNeonSites));

                foreach(NeonProductInfo neonProd in neonSite.dataProducts)
                {
                    if (supportedProductCodes.Contains(neonProd.dataProductCode))
                    {
                        var siteDataUrls = neonProd.availableDataUrls;

                        // prefer retrieving sensor-positions.csv in the most-recent data url:
                        siteDataUrls.Reverse();

                        NeonFileCollection dataFiles = null;
                        foreach (var dataUrl in siteDataUrls)
                        {
                            try
                            {
                                dataFiles = _apiReader.ReadNeonFilesFromApi(dataUrl);
                                break;
                            }
                            catch(Exception ex)
                            {
                                _log.LogWrite(dataUrl + " failed, try other month..");
                                if (dataUrl == siteDataUrls.Last())
                                {
                                    _log.LogWrite("GetSensorPositions ERROR downloading URL " + dataUrl + ". " + ex.Message);
                                }
                            }
                        }

                        //skip invalid dataFiles response if there is no data after all retries
                        if (dataFiles is null || dataFiles.files is null)
                        {
                            _log.LogWrite("GetSensorPositions dataUrl ERROR for site " + neonSiteCode + ", product " + neonProd.dataProductCode);
                            continue; // invalid dataFiles response, skip to next product.
                        }

                        foreach (var dataFile in dataFiles.files)
                        {
                            if (dataFile.name.Contains("sensor_positions"))
                            {
                                var sensorPositionUrl = dataFile.url;

                                if (_sensorPositionFileNames.Contains(dataFile.name))
                                {
                                    // prevent duplicate download of the same sensor_position file
                                    continue;
                                }

                                Console.WriteLine("sensors: " + dataFile.name);

                                var sensorPositions = senPosReader.ReadSensorPositionsFromUrl(sensorPositionUrl, neonSite);
                                foreach (var senPos in sensorPositions)
                                {
                                    string fullSiteCode = neonSiteCode + "_" + senPos.HorVerCode;

                                    if (!sensorSiteLookup.ContainsKey(fullSiteCode))
                                    {
                                        senPos.ParentSite = neonSite;
                                        senPos.neonProductCodes.Add(neonProd.dataProductCode);
                                        sensorSiteLookup.Add(fullSiteCode, senPos);
                                    }
                                    else
                                    {
                                        sensorSiteLookup[fullSiteCode].neonProductCodes.Add(neonProd.dataProductCode);
                                    }
                                }
                                // add to list of processed files so that we need not download the file twice.
                                _sensorPositionFileNames.Add(dataFile.name);
                            }
                        }

                    }
                }
            }
            
            return cuahsiSiteNeonSensorLookup;
        }


        /// <summary>
        /// Updates the NEON Sites using the NEON JSON Data API
        /// </summary>
        public void UpdateSites(CuahsiSiteNeonSensorLookup siteSensorLookup)
        {
            var neonSiteSensors = siteSensorLookup.Lookup;

            int numSitesFromApi = neonSiteSensors.Keys.Count;
            _log.LogWrite("UpdateSites for " + numSitesFromApi.ToString() + " sites ...");

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
                            _log.LogWrite("deleted old series from SeriesCatalog");
                        }
                        catch (Exception ex)
                        {
                            _log.LogWrite("error deleting old SeriesCatalog table: " + ex.Message);
                            return;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }

                    // delete old entries from "sites" table
                    // using batch delete 
                    DeleteOldSites(numSitesFromApi, connection);
                    

                    // to be adjusted
                    int batchSize = 10000;
                    long siteID = 0L;

                    //var siteList = neonSites.data;
                    int numBatches = (numSitesFromApi / batchSize) + 1;
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
                        if (batchEnd >= numSitesFromApi)
                        {
                            batchEnd = numSitesFromApi;
                        }

                        int i = 0;
                        foreach(var sensorKey in neonSiteSensors.Keys)
                        {
                            siteID = siteID + i;
                            var row = bulkTable.NewRow();
                            
                            var siteSensorCode = sensorKey;
                            var siteSensor = neonSiteSensors[sensorKey];

                            var horizontalIndex = siteSensor.HorVerCode.Split('.')[0];
                            var verticalIndex = siteSensor.HorVerCode.Split('.')[1];
                            var horizontal_vertical_offset = String.Format(", horizontal: {0}, vertical: {1}", horizontalIndex, verticalIndex);

                            var siteType = "Unknown";
                            if (verticalIndex.StartsWith("5"))
                            {
                                siteType = "Soil hole"; // soil array sensors always have index HOR=5xx.
                            }
                            else if (horizontalIndex == "000")
                            {
                                siteType = "Atmosphere"; // tower sensors always have index HOR==000.
                            }

                            //FIXME determine other site types from supported products or HOR/VER index.
                                

                            var siteCommentTmpl = "location: {0}|xOffset: {1}|yOffset: {2}|zOffset: {3}|roll: {4}|pitch: {5}|azimuth: {6}";
                            var siteComment = String.Format(siteCommentTmpl, siteSensor.HorVerCode, siteSensor.xOffset, 
                                siteSensor.yOffset, siteSensor.zOffset, siteSensor.roll, siteSensor.pitch, siteSensor.azimuth);

                            row["SiteID"] = siteID;
                            row["SiteCode"] = siteSensorCode;
                            row["SiteName"] = siteSensor.ParentSite.siteDescription + horizontal_vertical_offset;
                            row["Latitude"] = siteSensor.ReferenceLatitude;
                            row["Longitude"] = siteSensor.ReferenceLongitude;
                            row["LatLongDatumID"] = 3; // WGS1984
                            row["Elevation_m"] = siteSensor.ReferenceElevation;
                            row["VerticalDatum"] = "Unknown";
                            row["LocalX"] = siteSensor.xOffset;
                            row["LocalY"] = siteSensor.yOffset;
                            row["LocalProjectionID"] = DBNull.Value;
                            row["PosAccuracy_m"] = 1.0f;
                            row["State"] = siteSensor.ParentSite.stateName;
                            row["County"] = DBNull.Value;
                            row["Comments"] = siteComment;
                            row["SiteType"] = siteType;   
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
                _log.LogWrite("UpdateSites: " + numSitesFromApi.ToString() + " sites updated.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSites ERROR: " + ex.Message);
            }
        }
    }
}