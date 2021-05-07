using System;

namespace GldasHarvester
{
    public class GldasSite
    {
        private string _siteCode;
        private string _siteName;
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public long SiteID { get; set; }

        public GldasSite(long siteId, float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            SiteID = siteId;
            _siteCode = siteId.ToString("000000");
            _siteName = siteId.ToString("000000");
        }

        public string SiteCode
        {
            get
            {
                return _siteCode;
            }

        }
        public string SiteName
        {
            get
            {
                return _siteName;
            }

        }
    }
}
