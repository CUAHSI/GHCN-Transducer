using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCANHarvester
{
    class Variable
    {
        public int VariableID { get; set; }
        public string VariableCode { get; set; }
        public string VariableName { get; set; }
        public string VariableUnitsName { get; set; }
        public int VariableUnitsID { get; set; }
        public string SampleMedium { get; set; }             
        public string DataType { get; set; }
        public float TimeSupport { get; set; }
        public int TimeUnitsID { get; set; }

        public string ValueType { get { return "Field Observation"; } }
        public bool IsRegular { get { return true; } }

        public string Speciation { get { return ("Not Applicable"); } }
        public string GeneralCategory { get { return "Soil"; } }
        public double NoDataValue { get { return -9999.0; } }

    }
}
