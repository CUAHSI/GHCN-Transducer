using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNOTELHarvester
{
    class Series
    {
        public string MethodCode { get; set; }
        public int MethodID { get; set; }

        // the QC level is always id=0 ("quality controlled data")
        public int QualityControlLevelID { get { return 1; } }

        public long SiteID { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }

        public int VariableID { get; set; }
        public string VariableCode { get; set; }

        public DateTime BeginDateTime { get; set; }
        public DateTime BeginDateTimeUTC { get; set; }

        public DateTime EndDateTime { get; set; }
        public DateTime EndDateTimeUTC { get; set; }

        public int ValueCount { get; set; }

    }
}
