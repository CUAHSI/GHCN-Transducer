using System;

namespace GldasHarvester
{
    class Variable
    {
        public int VariableID { get; set; }
        public string VariableCode { get; set; }
        public string VariableName { get; set; }       
        public int VariableUnitsID { get; set; }
        public string VariableUnitsName { get; set; }
        public string SampleMedium { get; set; }
        public string DataType { get; set; }
        public int TimeUnitsID { get; set; }  // 104: Day in ODM Units table
        public float TimeSupport { get; set; }
        public bool IsRegular { get; set; }


        public string TimeUnitsName { get { return "hour"; } }
        public string ValueType { get { return "Field Observation"; } }              
        public string Speciation { get { return "Not Applicable"; } }
        public double NoDataValue { get { return -9999.0; } }
        public string GeneralCategory {  get { return "Climate"; } }

    }
}
