using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    public class NeonSite
    {
        public string siteDescription { get; set; }
        public double siteLongitude { get; set; }
        public string siteType { get; set; }
        public string stateName { get; set; }
        public string stateCode { get; set; }
        public double siteLatitude { get; set; }
        public string domainName { get; set; }
        public string domainCode { get; set; }
        public string siteCode { get; set; }
        public List<object> dataProducts { get; set; }
        public string siteName { get; set; }
    }

    public class NeonSiteCollection
    {
        public List<NeonSite> data { get; set; }
    }
}
