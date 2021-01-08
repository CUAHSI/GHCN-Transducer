using System.Net;
using System.Configuration;

namespace NEONHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ensure correct TLS settings.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set sleep time for downloading from API (in milliseconds).
            var sleepTime = int.Parse(ConfigurationManager.AppSettings["downloadSleepTimeMilliseconds"]);

            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            // (1) updating variables and methods from NEON data product info
            var varM = new VariableManager(logger, sleepTime);
            varM.UpdateVariables();

            // (2) updating methods from xlsx lookup table + product info
            var methodM = new MethodManager(logger, sleepTime);
            methodM.UpdateMethods();

            // (3) updating sites
            var siteM = new SiteManager(logger, sleepTime);
            var neonSiteCodeLookup = siteM.GetNeonSites();

            // (3a) retrieving sensor positions
            var neonSiteSensors = siteM.GetSensorPositions(neonSiteCodeLookup);

            // (3b) saving sites to ODM database
            siteM.UpdateSites(neonSiteSensors);
            
            // (4) updating sources TODO use distinct source per NEON site or product.
            var srcM = new SourceManager(logger);
            srcM.UpdateSources();

            // (5) updating the series catalog
            var seriesM = new SeriesCatalogManager(logger);
            seriesM.UpdateSeriesCatalog(neonSiteSensors);
        }
    }
}