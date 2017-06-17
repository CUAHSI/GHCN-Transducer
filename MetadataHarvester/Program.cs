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
            SiteHarvester h = new SiteHarvester();

            // (1) updating variables and sources
            h.UpdateVariables();
            h.UpdateSources();

            // (2) updating sites
            h.ReadCountries();
            h.ReadStates();
            h.ReadStations();
            h.UpdateSites();

            // (3) updating the series catalog..
            SeriesCatalogManager seriesManager = new SeriesCatalogManager();
            seriesManager.UpdateSeriesCatalog();
        }
    }
}
