using System;

namespace CoCoHarvester
{
    class Qualifier
    {
        public Qualifier(string code, string description)
        {
            QualifierCode = code;
            QualifierDescription = description;
        }

        public int QualifierID { get; set; }
        public string QualifierCode { get; set; }
        public string QualifierDescription { get; set; }
    }
}
