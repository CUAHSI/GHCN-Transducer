using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCANHarvester
{
    public class Site
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Elevation { get; set; }
        public string SiteName { get; set; }
        public long SiteID { get; set; }
        public string SiteCode { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string StationType { get; set; }
        public bool IsActive { get; set; }

        public DateTime PrecipStart { get; set; }
        public DateTime PrecipEnd { get; set; }

        public string Comments { get; set; }
    }
}
