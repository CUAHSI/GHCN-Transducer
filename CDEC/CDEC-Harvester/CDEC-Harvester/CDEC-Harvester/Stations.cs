using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    public class StationsModel
    {
        public string STA { get; set; }
        public string StationName { get; set; }
        public int Elevation { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string NearbyCity { get; set; }
        public int Hydro { get; set; }
        public int Basin { get; set; }
        public int Owner { get; set; }
        public int Maintenance { get; set; }
        public int Operator { get; set; }
        public int Map { get; set; }
        public int Collect { get; set; }
        public string CountyName { get; set; }
        public string BasinName { get; set; }
        public string AgencyName { get; set; }
        public string HydroArea { get; set; }
        public string Sensors { get; set; }
    }

    public class StationsModelClassMap : ClassMap<StationsModel>
    {
        public StationsModelClassMap()
        {
            Map(m => m.STA).Name("STA");
            Map(m => m.StationName).Name("Station Name");
            Map(m => m.Elevation).Name("Elevation");
            Map(m => m.Latitude).Name("Latitude");
            Map(m => m.Longitude).Name("Longitude");
            Map(m => m.NearbyCity).Name("Nearby City");
            Map(m => m.Hydro).Name("Hydro");
            Map(m => m.Basin).Name("Basin");
            Map(m => m.Owner).Name("Owner");
            Map(m => m.Maintenance).Name("Maintenance");
            Map(m => m.Operator).Name("Operator");
            Map(m => m.Map).Name("Map");
            Map(m => m.Collect).Name("Collect");
            Map(m => m.CountyName).Name("County Name");
            Map(m => m.BasinName).Name("Basin Name");
            Map(m => m.AgencyName).Name("Agency Name");
            Map(m => m.HydroArea).Name("Hydro Area");
            Map(m => m.Sensors).Name("Sensors");
        }
    }

}
