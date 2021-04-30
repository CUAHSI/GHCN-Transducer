using System;

namespace GldasHarvester
{
    class GldasQualifier
    {
        public GldasQualifier(string code, string description)
        {
            QualifierCode = code;
            QualifierDescription = description;
        }

        public int QualifierID { get; set; }
        public string QualifierCode { get; set; }
        public string QualifierDescription { get; set; }
    }
}