using System;

namespace CoCoHarvester
{
    class Variable
    {
        public int VariableID { get; set; }
        public string VariableCode { get; set; }
        public string VariableName { get; set; }       
        public int VariableUnitsID { get; set; }
        public string SampleMedium { get; set; }             
        public string DataType { get; set; }

        public string ValueType { get { return "Field Observation"; } }
        public bool IsRegular { get { return true; } }
        public int TimeSupport { get { return 1; } }
        public int TimeUnitsID { get { return 104; } } // 104: Day in ODM Units table
        public string Speciation { get { return ("Not Applicable"); } }
        public string GeneralCategory { get { return "Climate"; } }
        public double NoDataValue { get { return -9999.0; } }

    }
}
