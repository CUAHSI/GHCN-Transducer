using System;

namespace NEONHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            // (1) updating variables and methods from NEON data product info
            var varM = new VariableManager(logger);
            varM.UpdateVariables();
            varM.UpdateMethods();


            // (3) updating sites
            varM.GetSitePositions();

            var siteM = new SiteManager(logger);
            siteM.UpdateSites_fast();

            

            // (4) updating sources
            var srcM = new SourceManager(logger);
            srcM.UpdateSources();

            // (4) updating qualifiers
            var qualM = new QualifierManager(logger);
            qualM.UpdateQualifiers();

            // (5) updating the series catalog
            SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
            seriesM.UpdateSeriesCatalog_fast();
        }
    }
}