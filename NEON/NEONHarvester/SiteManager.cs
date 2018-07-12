using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

namespace NEONHarvester
{
    /// <summary>
    /// responsible for updating the sites in ODM
    /// </summary>
    class SiteManager
    {
        private LogWriter _log;
        private NeonApiReader _apiReader;

        public SiteManager(LogWriter log)
        {
            _log = log;
            _apiReader = new NeonApiReader(log);
        }

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


        public Dictionary<string, NeonSensorPosition> GetSensorPositions()
        {
            var siteInfoList = _apiReader.ReadSitesFromApi().data;
            var siteInfoLookup = new Dictionary<string, NeonSite>();
            foreach(NeonSite neonSite in siteInfoList)
            {
                siteInfoLookup.Add(neonSite.siteCode, neonSite);
            }

            var sensorSiteLookup = new Dictionary<string, NeonSensorPosition>();

            var senPosReader = new SensorPositionReader(_log);

            List<string> supportedProductCodes = new List<string>();
            var lookupReader = new LookupFileReader(_log);
            supportedProductCodes = lookupReader.ReadProductCodesFromExcel();

            foreach (string productCode in supportedProductCodes)
            {
                var siteDataUrls = new Dictionary<string, string>();
                siteDataUrls = GetSitesForProduct(productCode);

                foreach (var siteCode in siteDataUrls.Keys)
                {
                    var dataUrl = siteDataUrls[siteCode];
                    var dataFiles = _apiReader.ReadNeonFilesFromApi(dataUrl);
                    foreach (var dataFile in dataFiles.files)
                    {
                        if (dataFile.name.Contains("sensor_positions"))
                        {
                            Console.WriteLine(dataFile.name);
                            var sensorPositionUrl = dataFile.url;

                            var sensorPositions = senPosReader.ReadSensorPositionsFromUrl(sensorPositionUrl);
                            foreach (var senPos in sensorPositions)
                            {
                                string fullSiteCode = siteCode + "_" + senPos.HorVerCode;
                                
                                if (!sensorSiteLookup.ContainsKey(fullSiteCode))
                                {
                                    senPos.ParentSite = siteInfoLookup[siteCode];
                                    sensorSiteLookup.Add(fullSiteCode, senPos);
                                }
                            }
                        }
                    }
                }
            }
            return sensorSiteLookup;
        }


        /// <summary>
        /// Returns a dictionary with entries: Neon short site code: latest data URL
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetSitesForProduct(string productCode)
        {
            var neonProduct = _apiReader.ReadProductFromApi(productCode);
            var productSiteCodes = neonProduct.siteCodes;

            var siteDataUrls = new Dictionary<string, string>();
            foreach (var siteCode in productSiteCodes)
            {
                var shortCode = siteCode.siteCode;
                var dataUrls = siteCode.availableDataUrls;
                var lastDataUrl = dataUrls.Last();
                siteDataUrls.Add(shortCode, lastDataUrl);
            }
            return siteDataUrls;
        }



        /// <summary>
        /// Updates the NEON Sites using the NEON JSON Data API
        /// </summary>
        public void UpdateSites()
        {
            var neonSiteSensors = GetSensorPositions();

            NeonSiteCollection neonSites = _apiReader.ReadSitesFromApi();

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

                    var siteList = neonSites.data;
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
                            row["SiteID"] = siteID;
                            row["SiteCode"] = siteSensorCode;
                            row["SiteName"] = siteSensor.ParentSite.siteName;
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
                            row["Comments"] = siteSensor.ParentSite.siteDescription;
                            row["SiteType"] = "Atmosphere"; // from CUAHSI SiteTypeCV controlled vocabulary
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