using System;
using System.Collections.Generic;
using System.Net;

namespace USBRHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            var catalogTimeseriesItems = new List<USBRCatalogItem.Data>();
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

            if (catalogRecords != null) {
                catalogTimeseriesItems = RISE_APIM.GetCatalogItemsAsync(catalogRecords).Result;
                logger.LogWrite(String.Format("Found {0} distinct timeseries items", catalogTimeseriesItems.Count));
            }

            // (2) updating variables
            //var varM = new VariableManager(logger);
            //varM.UpdateVariables();

                // (3) updating methods
                //var methM = new MethodManager(logger);
                //methM.UpdateMethods();

                // (4) updating sources
                //var srcM = new SourceManager(logger);
                //srcM.UpdateSources();

                // (4) updating qualifiers
                //var qualM = new QualifierManager(logger);
                // qualM.UpdateQualifiers();

                // (5) updating the series catalog
                //SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
                //seriesM.UpdateSeriesCatalog_fast();
        }
    }
}