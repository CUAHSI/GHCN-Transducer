using System;

namespace GhcnHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // (0) setup progress logging
            var logger = new LogWriter();
            
            // (1) updating variables and sources
            var varM = new VariableManager(logger);
            varM.UpdateVariables();

            // (2) updating sources
            var srcM = new SourceManager(logger);
            srcM.UpdateSources();

            // (3) updating qualifiers
            var qualM = new QualifierManager(logger);
            qualM.UpdateQualifiers();

            // (4) updating sites
            var siteM = new SiteManager(logger);
            siteM.UpdateSites_fast();

            // (5) updating the series catalog
            SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
            seriesM.UpdateSeriesCatalog_fast();
        }
    }
}