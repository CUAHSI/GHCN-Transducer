using System.Collections.Generic;

namespace NEONHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            //var tester = new WebServiceTester("http://hydroportal.cuahsi.org/NEON/cuahsi_1_1.asmx");
            //tester.Run();

            // (1) updating variables and methods from NEON data product info
            var varM = new VariableManager(logger);
            varM.UpdateVariables();

            // (2) updating methods from xlsx lookup table + product info
            var methodM = new MethodManager(logger);
            methodM.UpdateMethods();

            // (3) updating sites
            var siteM = new SiteManager(logger);
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