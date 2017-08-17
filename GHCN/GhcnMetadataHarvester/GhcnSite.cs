using System;

namespace GhcnHarvester
{
    public class GhcnSite
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Elevation { get; set; }
        public string SiteName { get; set; }
        public long SiteID { get; set; }
        public string SiteCode { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public int? WmoID { get; set; }
        public string HCNFlag { get; set; }
        public bool GSN { get; set; }
        public bool CoCoRaHS { get; set; }
        public bool Snotel { get; set; }
        public string NetworkFlag { get; set; }
    }
}
