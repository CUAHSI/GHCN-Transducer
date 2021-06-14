using System;
using System.Collections.Generic;
using System.Net;
using static USBRHarvester.USBRLocationPoint;

namespace USBRHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            var catalogTimeseriesItems = new List<USBRCatalogItem.Data>();
            var catalogLocations = new List<USBRLocationPointRoot>();
            var catalogItemsWithPointLocation = new List<ItemLocationParameter>();
            // Ensure correct TLS settings.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            // (1) retrieving catalog items
            var RISE_APIM = new RISE_ApiManager(logger);
            //var catalogItems = RISE_APIM.GetCatalogItems();
            //call with callback if additional processsing required
            //var catalogRecords = RISE_APIM.GetCatalogRecordsAsync(RISE_APIM.ResultCallBack).Result;
            
            var catalogRecords = RISE_APIM.GetCatalogRecordsAsync().GetAwaiter().GetResult();

            logger.LogWrite(String.Format("Found {0} distinct USBRCatalogRecord.", catalogRecords.Count));
            var parameters = RISE_APIM.GetParametersAsync().GetAwaiter().GetResult();

            if (catalogRecords != null) {

                
                var result = RISE_APIM.GetCatalogItemsAsync(catalogRecords).GetAwaiter().GetResult();
                ////Get timeseries
                catalogTimeseriesItems = result.Item1;
                catalogItemsWithPointLocation = result.Item2;
                logger.LogWrite(String.Format("Found {0} distinct timeseries items", catalogItemsWithPointLocation.Count));
                ////get locations
                catalogLocations = result.Item3;
                logger.LogWrite(String.Format("Found {0} distinct locations", catalogLocations.Count));
                
            }

            // (1) updating sites
            var siteM = new SiteManager(logger);
            siteM.UpdateSites_fast(catalogLocations);


            // (2) updating variables mapping to parameters
            if (parameters != null)
            {
                var varM = new VariableManager(logger);
                varM.UpdateVariables(parameters);
            }
            

                // (3) updating methods
                //var methM = new MethodManager(logger);
                //methM.UpdateMethods();

                // (4) updating sources
                var srcM = new SourceManager(logger);
                srcM.UpdateSources();

                // (4) updating qualifiers
                //var qualM = new QualifierManager(logger);
                // qualM.UpdateQualifiers();

                // (5) updating the series catalog
                SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
                seriesM.UpdateSeriesCatalog_fast(catalogRecords, catalogTimeseriesItems, catalogItemsWithPointLocation);
        }
    }
}