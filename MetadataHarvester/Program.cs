using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetadataHarvester
{
    class Program
    {
        static void Main(string[] args)
        {
            // (1) updating variables and sources
            var varM = new VariableManager();
            varM.UpdateVariables();

            // (2) updating sources
            var srcM = new SourceManager();
            srcM.UpdateSources();

            // (3) updating sites
            var siteM = new SiteManager();
            //siteM.UpdateSites_fast();

            // (4) updating the series catalog
            SeriesCatalogManager seriesM = new SeriesCatalogManager();
            seriesM.UpdateSeriesCatalog_fast();
        }
    }
}
