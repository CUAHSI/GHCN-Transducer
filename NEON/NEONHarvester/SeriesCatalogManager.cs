using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace NEONHarvester
{
    /// <summary>
    /// Responsible for updating SeriesCatalog in the ODM.
    /// </summary>
    class SeriesCatalogManager
    {
        private LogWriter _log;

        public SeriesCatalogManager(LogWriter log)
        {
            _log = log;
        }

        /// <summary>
        /// Recreate the SeriesCatalog table in the ODM database using sensor position info, NEON sites API response,
        /// methods ODM table, Sites ODM table, Sources ODM table.
        /// </summary>
        /// <param name="neonSiteSensors">Lookup class containing Cuahsi Site Code --> NeonSensorPosition dictionary.</param>
        public void UpdateSeriesCatalog(CuahsiSiteNeonSensorLookup neonSiteSensors)
        {
            List<CuahsiTimeSeries> fullSeriesList = new List<CuahsiTimeSeries>(1000);

            try
            {
                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    var sitesFromDB = GetSitesFromDB(connection);
                    var supportedVariables = GetProductVariableLookupFromDb(connection);
                    var supportedMethods = GetProductMethodLookupFromDb(connection);
                    var source = GetSourceFromDB(connection);

                    var i = 0;
                    foreach (Site site in sitesFromDB)
                    {

                        _log.LogWrite("Harvesting series for site: " + i.ToString() + " " + site.SiteCode);
                        try
                        {
                            var neonSensor = neonSiteSensors.Lookup[site.SiteCode];
                            var neonSite = neonSensor.ParentSite;
                            var productsAtSensor = neonSensor.neonProductCodes;

                            List<CuahsiTimeSeries> siteSeriesList = GetListOfSeriesForSite(site, neonSite, productsAtSensor, supportedVariables, supportedMethods, source);
                            fullSeriesList.AddRange(siteSeriesList);
                            i++;
                        }
                        catch (Exception ex)
                        {
                            _log.LogWrite("ERROR harvesting series for site " + site.SiteCode + " " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
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
                        _log.LogWrite("UpdateSeriesCatalog: error deleting old series from SeriesCatalog " + ex.Message);
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
                                row["SiteType"] = s.SiteType;
                                row["VariableID"] = s.VariableID;
                                row["VariableCode"] = s.VariableCode;
                                row["VariableName"] = s.VariableName;
                                row["Speciation"] = s.Speciation;
                                row["VariableUnitsID"] = s.VariableUnitsID;
                                row["VariableUnitsName"] = s.VariableUnitsName;
                                row["SampleMedium"] = s.SampleMedium;
                                row["ValueType"] = s.ValueType;
                                row["TimeSupport"] = 30;
                                row["TimeUnitsID"] = s.TimeUnitsID;
                                row["TimeUnitsName"] = s.TimeUnitsName;
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
        /// Given a month in format YYYY-mm, get the date of first day 00:00z of the month.
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
        /// Given a month in format YYYY-mm, 
        /// get the datetime of the last day of the month at 23:30z.
        /// </summary>
        /// <param name="neonMonth">For example 2017-11</param>
        /// <returns>For example 2017-11-30T23:30:00Z</returns>
        private DateTime EndTimeFromNeonMonth(string neonMonth)
        {
            var year = Convert.ToInt32(neonMonth.Split('-')[0]);
            var month = Convert.ToInt32(neonMonth.Split('-')[1]);

            // Number of days in a given month also depends on the year (leap vs. non-leap years).
            return new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 30, 0);
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
                        while (reader.Read())
                        {
                            var site = new Site();
                            site.SiteID = Convert.ToInt64(reader["SiteID"]);
                            site.SiteCode = Convert.ToString(reader["SiteCode"]);
                            site.SiteName = Convert.ToString(reader["SiteName"]);
                            site.Latitude = Convert.ToDecimal(reader["Latitude"]);
                            site.Longitude = Convert.ToDecimal(reader["Longitude"]);
                            site.Elevation = Convert.ToDecimal(reader["Elevation_m"]);
                            site.SiteType = Convert.ToString(reader["SiteType"]);
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



        /// <summary>
        /// Get a lookup NEON Product -> Cuahsi Method from the database for all supported products.
        /// </summary>
        /// <param name="connection">ODM database connection</param>
        /// <returns>a dictionary of NEON product code -> CUAHSI method object.</returns>
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


        /// <summary>
        /// Associates a NEON product, CUAHSI variable code, and CUAHSI variable object.
        /// One NEON product may be mapped to multiple CUAHSI variable.
        /// For example, the "wind speed and direction " product is mapped to "windSpeedMean" and "windDirMean variable."
        /// </summary>
        /// <param name="connection">The database connection object.</param>
        /// <returns>A dictionary of dictionaries NEONProductCode -> { CUAHSIVariableCode: CuahsiVariable }</returns>
        public ProductVariableLookup GetProductVariableLookupFromDb(SqlConnection connection)
        {
            var lookup = new Dictionary<string, Dictionary<string, Variable>>();

            var variableList = new List<Variable>();

            // fetch information from ODM (Variables joined with Units table)
            string sqlQuery = @"SELECT v.VariableID, v.VariableCode,  v.VariableName, 
v.SampleMedium,v.TimeUnitsID, v.DataType, v.GeneralCategory, v.VariableUnitsID, v.ValueType, v.Speciation, 
tu.UnitsName AS TimeUnitsName, u.UnitsName AS VariableUnitsName 
FROM dbo.Variables v 
INNER JOIN dbo.Units u ON v.VariableUnitsID = u.UnitsID
INNER JOIN dbo.Units tu ON v.TimeUnitsID = tu.UnitsID";

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
                            v.SampleMedium = Convert.ToString(reader["SampleMedium"]);
                            v.TimeUnitsID = Convert.ToInt32(reader["TimeUnitsID"]);
                            v.TimeUnitsName = Convert.ToString(reader["TimeUnitsName"]);
                            v.DataType = Convert.ToString(reader["DataType"]);
                            v.GeneralCategory = Convert.ToString(reader["GeneralCategory"]);
                            v.VariableUnitsID = Convert.ToInt32(reader["VariableUnitsID"]);
                            v.VariableUnitsName = Convert.ToString(reader["VariableUnitsName"]);
                            v.ValueType = Convert.ToString(reader["ValueType"]);
                            v.Speciation = Convert.ToString(reader["Speciation"]);

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


        public Source GetSourceFromDB(SqlConnection connection)
        {
            var s = new Source();

            // TODO: retrieve a source by source code.
            // For citation purposes, a source should be probably associated with a NEON site.
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
        /// Retrieves a list of available time serieses for a CUAHSI site.
        /// </summary>
        /// <param name="cuahsiSite">The CUAHSI site object from the ODM database with SiteID and SiteCode.</param>
        /// <param name="supportedProductCodes">A list of ALL NEON product codes supported by CUAHSI (from the XLSX lookup table)</param>
        /// <returns>Series List</returns>
        public List<CuahsiTimeSeries> GetListOfSeriesForSite(Site cuahsiSite, NeonSite neonSite, List<string> productsAtSensor, ProductVariableLookup supportedProducts, ProductMethodLookup supportedMethods, Source source)
        {
            var seriesList = new List<CuahsiTimeSeries>();

            var siteDataProducts = neonSite.dataProducts;

            foreach (NeonProductInfo siteProduct in siteDataProducts)
            {
                string productCode = siteProduct.dataProductCode;
                if (productsAtSensor.Contains(productCode) && supportedProducts.Lookup.ContainsKey(productCode) && supportedMethods.Lookup.ContainsKey(productCode))
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
                        s.SiteType = cuahsiSite.SiteType;
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
                        s.ValueType = v.ValueType;
                        s.Speciation = v.Speciation;
                        s.DataType = v.DataType;

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

                        if (availableMonths.Count == 0)
                        {
                            _log.LogWrite(string.Format("ERROR harvesting site {0}, product {1}. No available months from NEON API.", s.SiteCode, productCode));
                        }
                        else
                        {
                            var estimatedValueCount = 0;
                            var valuesPerDay = 48; // for 30-minute data there are 2*24=48 values per day.
                            availableMonths.Sort();
                            foreach (var availableMonth in availableMonths)
                            {
                                var monthBeginTime = BeginTimeFromNeonMonth(availableMonth);
                                var monthEndTime = EndTimeFromNeonMonth(availableMonth);
                                var spanDays = (monthEndTime - monthBeginTime).TotalDays;
                                estimatedValueCount += Convert.ToInt32(Math.Round(spanDays) *valuesPerDay);
                            }

                            s.BeginDateTime = BeginTimeFromNeonMonth(availableMonths.First());
                            s.EndDateTime = EndTimeFromNeonMonth(availableMonths.Last());
                            s.BeginDateTimeUTC = BeginTimeFromNeonMonth(availableMonths.First());
                            s.EndDateTimeUTC = EndTimeFromNeonMonth(availableMonths.Last());

                            s.ValueCount = estimatedValueCount;

                            seriesList.Add(s);
                        }
                    }
                }
            }
            return seriesList;
        }
    }
}
