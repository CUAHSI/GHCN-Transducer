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
            h.UpdateVariables();
            h.UpdateSources();

            SeriesCatalogManager seriesManager = new SeriesCatalogManager();
            seriesManager.UpdateSeriesCatalog();

            // read countries from the gncnd-countries.txt file
            h.ReadCountries();
            h.ReadStates();
            h.ReadStations();
            h.UpdateSites();
        }
    }
}
