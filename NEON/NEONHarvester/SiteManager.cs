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

        public List<Site> GetSitesFromDB(SqlConnection connection)
        {
            var siteList = new List<Site>();
            string sqlQuery = "SELECT * FROM dbo.Sites";
            using (var cmd = new SqlCommand(sqlQuery, connection))
            {
                try
                {
                    connection.Open();
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while(reader.Read())
                        {
                            var site = new Site();
                            site.SiteID = Convert.ToInt64(reader["SiteID"]);
                            site.SiteCode = Convert.ToString(reader["SiteCode"]);
                            site.Latitude = Convert.ToDecimal(reader["Latitude"]);
                            site.Longitude = Convert.ToDecimal(reader["Longitude"]);
                            site.Elevation = Convert.ToDecimal(reader["Elevation_m"]);
                            siteList.Add(site);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    var msg = "ERROR reading sites from DB: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return siteList;
                }
                finally
                {
                    connection.Close();
                }
            }
            return siteList;
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
        /// 
        /// </summary>
        /// <param name="neonMonth">For example 2017-11</param>
        /// <returns>For example 2017-11-01T00:00:00Z</returns>
        private DateTime BeginTimeFromNeonMonth(string neonMonth)
        {
            var year = Convert.ToInt32(neonMonth.Split('-')[0]);
            var month = Convert.ToInt32(neonMonth.Split('-')[1]);
            return new DateTime(year, month, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="neonMonth">For example 2017-11</param>
        /// <returns>For example 2017-11-01T00:00:00Z</returns>
        private DateTime EndTimeFromNeonMonth(string neonMonth)
        {
            var year = Convert.ToInt32(neonMonth.Split('-')[0]);
            var month = Convert.ToInt32(neonMonth.Split('-')[1]);
            return new DateTime(year, month, 28); //TODO need better method to find the 
        }


        public Source GetSourceFromDB(SqlConnection connection)
        {
            var s = new Source();

            string sqlQuery = "SELECT * FROM dbo.Sources";
            using (var cmd = new SqlCommand(sqlQuery, connection))
            {
                try
                {
                    connection.Open();
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            s.SourceID = Convert.ToInt32(reader["SourceID"]);
                            s.SourceCode = Convert.ToString(reader["SourceCode"]);
                            s.Citation = Convert.ToString(reader["Citation"]);
                            s.Organization = Convert.ToString(reader["Organization"]);
                            s.SourceDescription = Convert.ToString(reader["SourceDescription"]);

                        }
                    }

                }
                catch (Exception ex)
                {
                    var msg = "ERROR reading source from DB: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
            return s;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>A dictionary of dictionaries NEONProductCode -> { CUAHSIVariableCode: CuahsiVariable }</returns>
        public ProductVariableLookup GetProductVariableLookupFromDb(SqlConnection connection)
        {
            var lookup = new Dictionary<string, Dictionary<string, Variable>>();

            var variableList = new List<Variable>();
            string sqlQuery = "SELECT * FROM dbo.Variables";
            using (var cmd = new SqlCommand(sqlQuery, connection))
            {
                try
                {
                    connection.Open();
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var v = new Variable();
                            v.VariableID = Convert.ToInt32(reader["VariableID"]);
                            v.VariableCode = Convert.ToString(reader["VariableCode"]);
                            v.VariableName = Convert.ToString(reader["VariableName"]);
                            // sample medium, etc..

                            var neonProductCode = v.GetNeonProductCode();
                            if (lookup.ContainsKey(neonProductCode))
                            {
                                // insert variable into the nested dictionary ...
                                var productVariables = lookup[neonProductCode];
                                if (!(productVariables.ContainsKey(v.VariableCode)))
                                {
                                    productVariables.Add(v.VariableCode, v);
                                }
                            }
                            else
                            {
                                // insert new product into the top-level dictionary
                                var productVariables = new Dictionary<string, Variable>();
                                productVariables.Add(v.VariableCode, v);
                                lookup.Add(neonProductCode, productVariables);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    var msg = "ERROR reading product-variable lookup from DB: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
            var result = new ProductVariableLookup();
            result.Lookup = lookup;
            return result;
        }


        /// <summary>
        /// Get a lookup NEON Product -> Cuahsi Method from the database for all supported products.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ProductMethodLookup GetProductMethodLookupFromDb(SqlConnection connection)
        {
            var lookup = new Dictionary<string, MethodInfo>();

            var methodList = new List<MethodInfo>();
            string sqlQuery = "SELECT * FROM dbo.Methods";
            using (var cmd = new SqlCommand(sqlQuery, connection))
            {
                try
                {
                    connection.Open();
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var m = new MethodInfo();
                            m.MethodID = Convert.ToInt32(reader["MethodID"]);
                            m.MethodCode = Convert.ToString(reader["MethodCode"]);
                            m.MethodLink = Convert.ToString(reader["MethodLink"]);
                            m.MethodDescription = Convert.ToString(reader["MethodDescription"]);

                            var neonProductCode = m.MethodCode;
                            if (!lookup.ContainsKey(neonProductCode))
                            {
                                lookup.Add(neonProductCode, m);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    var msg = "ERROR reading product-variable lookup from DB: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
            var result = new ProductMethodLookup();
            result.Lookup = lookup;
            return result;
        }



        public void UpdateSeriesCatalog()
        {
            List<CuahsiTimeSeries> fullSeriesList = new List<CuahsiTimeSeries>();

            try
            {
                

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    var sitesFromDB = GetSitesFromDB(connection);
                    var supportedVariables = GetProductVariableLookupFromDb(connection);
                    var supportedMethods = GetProductMethodLookupFromDb(connection);
                    var source = GetSourceFromDB(connection);

                    foreach (Site site in sitesFromDB)
                    {
                        List<CuahsiTimeSeries> siteSeriesList = GetListOfSeriesForSite(site, supportedVariables, supportedMethods, source);
                        fullSeriesList.AddRange(siteSeriesList);
                    }
                }
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateSeriesCatalog ERROR" + ex.Message);
            }

            Console.WriteLine("updating series catalog for " + fullSeriesList.Count.ToString() + " series ...");

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString))
            {
                // delete old entries from series catalog
                string sql = "TRUNCATE TABLE dbo.SeriesCatalog";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    try
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("deleted old series from SeriesCatalog");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error deleting old series from SeriesCatalog");
                        _log.LogWrite("UpdateSeriesCatalog: error deleting old series from SeriesCatalog");
                        return;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }

                int batchSize = 500;
                int numBatches = (fullSeriesList.Count / batchSize) + 1;
                long seriesID = 0L;

                try
                {
                    for (int b = 0; b < numBatches; b++)
                    {
                        // prepare for bulk insert
                        DataTable bulkTable = new DataTable();
                        bulkTable.Columns.Add("SeriesID", typeof(long));
                        bulkTable.Columns.Add("SiteID", typeof(long));
                        bulkTable.Columns.Add("SiteCode", typeof(string));
                        bulkTable.Columns.Add("SiteName", typeof(string));
                        bulkTable.Columns.Add("SiteType", typeof(string));
                        bulkTable.Columns.Add("VariableID", typeof(int));
                        bulkTable.Columns.Add("VariableCode", typeof(string));
                        bulkTable.Columns.Add("VariableName", typeof(string));
                        bulkTable.Columns.Add("Speciation", typeof(string));
                        bulkTable.Columns.Add("VariableUnitsID", typeof(int));
                        bulkTable.Columns.Add("VariableUnitsName", typeof(string));
                        bulkTable.Columns.Add("SampleMedium", typeof(string));
                        bulkTable.Columns.Add("ValueType", typeof(string));
                        bulkTable.Columns.Add("TimeSupport", typeof(float));
                        bulkTable.Columns.Add("TimeUnitsID", typeof(int));
                        bulkTable.Columns.Add("TimeUnitsName", typeof(string));
                        bulkTable.Columns.Add("DataType", typeof(string));
                        bulkTable.Columns.Add("GeneralCategory", typeof(string));
                        bulkTable.Columns.Add("MethodID", typeof(int));
                        bulkTable.Columns.Add("MethodDescription", typeof(string));
                        bulkTable.Columns.Add("SourceID", typeof(int));
                        bulkTable.Columns.Add("Organization", typeof(string));
                        bulkTable.Columns.Add("SourceDescription", typeof(string));
                        bulkTable.Columns.Add("Citation", typeof(string));
                        bulkTable.Columns.Add("QualityControlLevelID", typeof(int));
                        bulkTable.Columns.Add("QualityControlLevelCode", typeof(string));
                        bulkTable.Columns.Add("BeginDateTime", typeof(DateTime));
                        bulkTable.Columns.Add("EndDateTime", typeof(DateTime));
                        bulkTable.Columns.Add("BeginDateTimeUTC", typeof(DateTime));
                        bulkTable.Columns.Add("EndDateTimeUTC", typeof(DateTime));
                        bulkTable.Columns.Add("ValueCount", typeof(int));

                        int batchStart = b * batchSize;
                        int batchEnd = batchStart + batchSize;
                        if (batchEnd >= fullSeriesList.Count)
                        {
                            batchEnd = fullSeriesList.Count;
                        }
                        for (int i = batchStart; i < batchEnd; i++)
                        {
                            try
                            {
                                var s = fullSeriesList[i];
                                var variableCode = s.VariableCode;
                                var siteCode = s.SiteCode;
                                var methodCode = s.MethodCode;

                                var row = bulkTable.NewRow();
                                seriesID = seriesID + 1;

                                row["SeriesID"] = seriesID;
                                row["SiteID"] = s.SiteID;
                                row["SiteCode"] = s.SiteCode;
                                row["SiteName"] = s.SiteName;
                                row["SiteType"] = "Atmosphere";
                                row["VariableID"] = s.VariableID;
                                row["VariableCode"] = s.VariableCode;
                                row["VariableName"] = s.VariableName;
                                row["Speciation"] = s.Speciation;
                                row["VariableUnitsID"] = s.VariableUnitsID;
                                row["VariableUnitsName"] = s.VariableUnitsName;
                                row["SampleMedium"] = s.SampleMedium;
                                row["ValueType"] = s.ValueType;
                                row["TimeSupport"] = s.TimeSupport;
                                row["TimeUnitsID"] = s.TimeUnitsID;
                                row["TimeUnitsName"] = "Day"; // todo get from DB !!!
                                row["DataType"] = s.DataType;
                                row["GeneralCategory"] = s.GeneralCategory;
                                row["MethodID"] = s.MethodID;
                                row["MethodDescription"] = s.MethodDescription;
                                row["SourceID"] = s.SourceID;
                                row["Organization"] = s.Organization;
                                row["SourceDescription"] = s.SourceDescription;
                                row["Citation"] = s.Citation;
                                row["QualityControlLevelID"] = 1;
                                row["QualityControlLevelCode"] = "1";
                                row["BeginDateTime"] = s.BeginDateTime;
                                row["EndDateTime"] = s.EndDateTime;
                                row["BeginDateTimeUTC"] = s.BeginDateTime;
                                row["EndDateTimeUTC"] = s.EndDateTime;
                                row["ValueCount"] = s.ValueCount;
                                bulkTable.Rows.Add(row);

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
                        bulkCopy.DestinationTableName = "dbo.SeriesCatalog";
                        connection.Open();
                        bulkCopy.WriteToServer(bulkTable);
                        connection.Close();
                        Console.WriteLine("SeriesCatalog inserted row " + batchEnd.ToString());
                    }
                    Console.WriteLine("UpdateSeriesCatalog: " + fullSeriesList.Count.ToString() + " series updated.");
                    _log.LogWrite("UpdateSeriesCatalog: " + fullSeriesList.Count.ToString() + " series updated.");
                }
                catch (Exception ex)
                {
                    _log.LogWrite("UpdateSeriesCatalog ERROR: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Retrieves a list of available time serieses for a CUAHSI site.
        /// </summary>
        /// <param name="cuahsiSite">The CUAHSI site object from the ODM database with SiteID and SiteCode.</param>
        /// <param name="supportedProductCodes">A list of ALL NEON product codes supported by CUAHSI (from the XLSX lookup table)</param>
        /// <returns>Series List</returns>
        public List<CuahsiTimeSeries> GetListOfSeriesForSite(Site cuahsiSite, ProductVariableLookup supportedProducts, ProductMethodLookup supportedMethods, Source source)
        {
            var seriesList = new List<CuahsiTimeSeries>();
            
            // Get NEON site Code
            var neonSiteCode = cuahsiSite.GetNeonSiteCode();

            // Read Site info from NEON API
            NeonSite neonSite = _apiReader.ReadSiteFromApi(neonSiteCode);

            // Filter products available at a NEON site by products supported by CUAHSI

            // supportedProductCodes can be read by a dedicated function VariableManager.GetProductVariableLookupFromDb

            //List<string> supportedProductCodes = new List<string>();
            //var lookupReader = new LookupFileReader(_log);
            //supportedProductCodes = lookupReader.ReadProductCodesFromExcel();

            var siteDataProducts = neonSite.dataProducts;

            foreach(NeonProductInfo siteProduct in siteDataProducts)
            {
                string productCode = siteProduct.dataProductCode;
                if (supportedProducts.Lookup.ContainsKey(productCode) && supportedMethods.Lookup.ContainsKey(productCode))
                {
                    // get available months
                    var availableMonths = siteProduct.availableMonths;

                    Dictionary<string, Variable> supportedVariables = supportedProducts.Lookup[productCode];
                    foreach (Variable v in supportedVariables.Values)
                    {
                        var s = new CuahsiTimeSeries();
                        s.SiteID = cuahsiSite.SiteID;
                        s.SiteCode = cuahsiSite.SiteCode;
                        s.SiteName = cuahsiSite.SiteName;
                        s.SiteType = "Atmosphere";
                        s.VariableID = v.VariableID;
                        s.VariableCode = v.VariableCode;
                        s.VariableName = v.VariableName;
                        s.VariableUnitsID = v.VariableUnitsID;
                        s.VariableUnitsName = v.VariableUnitsName;
                        s.TimeUnitsID = v.TimeUnitsID;
                        s.TimeUnitsName = v.TimeUnitsName;
                        s.SampleMedium = v.SampleMedium;
                        s.GeneralCategory = v.GeneralCategory;
                        s.TimeSupport = v.TimeSupport;

                        var productMethod = supportedMethods.Lookup[productCode];
                        s.MethodID = productMethod.MethodID;
                        s.MethodCode = productMethod.MethodCode;
                        s.MethodDescription = productMethod.MethodDescription;

                        s.SourceID = source.SourceID;
                        s.Organization = source.Organization;
                        s.Citation = source.Citation;
                        s.SourceDescription = source.SourceDescription;

                        s.QualityControlLevelID = 1;
                        s.QualityControlLevelCode = "1"; //quality controlled data

                        s.BeginDateTime = BeginTimeFromNeonMonth(availableMonths[0]);
                        s.EndDateTime = EndTimeFromNeonMonth(availableMonths[1]);

                        s.BeginDateTimeUTC = s.BeginDateTime;
                        s.EndDateTimeUTC = s.EndDateTime;

                        seriesList.Add(s);
                    }
                }              
            }
            return seriesList;
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