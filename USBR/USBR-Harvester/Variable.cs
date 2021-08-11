using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBRHarvester
{
    class Variable
    {
        public int VariableID { get; set; }
        public string VariableCode { get; set; }
        public string VariableName { get; set; }
        public string VariableUnitsName { get; set; }
        public int VariableUnitsID { get; set; }
        public string SampleMedium { get { return ("Unknown"); } }
        public string DataType { get; set; }
        public float TimeSupport { get; set; }
        public int TimeUnitsID { get; set; } //arbitrarily set to 104 = day as there is no n/a

        public string ValueType { get { return "Field Observation"; } }
        public bool IsRegular { get { return false; } }

        public string Speciation { get { return ("Not Applicable"); } }
        public string GeneralCategory { get { return ("Unknown"); } }
        public double NoDataValue { get { return -9999; } }

    }
}
