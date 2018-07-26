using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    public class NeonTimeSeries
    {
        public long CuahsiSiteID { get; set; }
        public string CuahsiSiteCode { get; set; }
        public string NeonProductCode { get; set; }
        public int CuahsiMethodID { get; set; }
        public int CuahsiVariableID { get; set; }
        public string[] NeonAvailableMonths { get; set; }
    }
}
