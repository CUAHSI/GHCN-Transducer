using System;

namespace SNOTELHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            // (1) updating sites
            //var siteM = new SiteManager(logger);
            //siteM.UpdateSites_fast();

            // (2) updating variables
            var varM = new VariableManager(logger);
            varM.UpdateVariables();

            // (3) updating methods
            var methM = new MethodManager(logger);
            methM.UpdateMethods();

            // (4) updating sources
            var srcM = new SourceManager(logger);
            srcM.UpdateSources();

            // (4) updating qualifiers
            //var qualM = new QualifierManager(logger);
            //qualM.UpdateQualifiers();

            // (5) updating the series catalog
            //SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
            //seriesM.UpdateSeriesCatalog_fast();
        }
    }
}