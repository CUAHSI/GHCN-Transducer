using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    class CuahsiTimeSeries
    {
        public long SeriesID { get; set; }
        public long SiteID { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public string SiteType { get; set; }
        public int VariableID { get; set; }
        public string VariableCode { get; set; }
        public string VariableName { get; set; }
        public string Speciation { get; set; }
        public int VariableUnitsID { get; set; }
        public string VariableUnitsName { get; set; }
        public string SampleMedium { get; set; }
        public string ValueType { get; set; }
        public float TimeSupport { get; set; }
        public int TimeUnitsID { get; set; }
        public string TimeUnitsName { get; set; } // set to 30 minutes!
        public string DataType { get; set; }
        public string GeneralCategory { get; set; }
        public double NoDataValue { get; set; }

        public int MethodID { get; set; }
        public string MethodCode { get; set; }
        public string MethodDescription { get; set; }

        public int SourceID { get; set; }
        public string Organization { get; set; }
        public string SourceDescription { get; set; }
        public string Citation { get; set; }
        public int QualityControlLevelID { get; set; }
        public string QualityControlLevelCode { get; set; }
        public DateTime BeginDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime BeginDateTimeUTC { get; set; }
        public DateTime EndDateTimeUTC { get; set; }
        public int ValueCount { get; set; }
    }
}
