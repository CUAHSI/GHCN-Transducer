using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    public class SensorModel
    {
        public int SENSORNUM { get; set; }
        public string SENSOR { get; set; }
        public string PECODE { get; set; }
        public string DESCRIPTION { get; set; }
        public string UNITS { get; set; }
        public string ODM_Variable_term { get; set; }
        public string ODM_Unit_abbre { get; set; }

    }

    public class SensorModelClassMap : ClassMap<SensorModel>
    {
        public SensorModelClassMap()
        {
            Map(m => m.SENSORNUM).Name("SENSOR NUM");
            Map(m => m.SENSOR).Name("SENSOR");
            Map(m => m.PECODE).Name("PE CODE");
            Map(m => m.DESCRIPTION).Name("DESCRIPTION");
            Map(m => m.UNITS).Name("UNITS");
            Map(m => m.ODM_Variable_term).Name("ODM Variable term");
            Map(m => m.ODM_Unit_abbre).Name("ODM Unit abbre");
        }
    }

}
