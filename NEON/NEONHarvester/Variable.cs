namespace NEONHarvester
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
        public string TimeUnitsName { get; set; }

        public string ValueType { get; set; }
        public bool IsRegular { get { return true; } }

        public string Speciation { get; set; } 
        public string GeneralCategory { get; set; }
        public double NoDataValue { get { return -9999.0; } }

        /// <summary>
        /// Retrieves a NEON product code from a CUAHSI variable code
        /// </summary>
        /// <returns>NEON product code, for example DP1.00001.001_windSpeedMean --> DP1.00001.001</returns>
        public string GetNeonProductCode()
        {
            return VariableCode.Split('_')[0];
        }

    }
}
