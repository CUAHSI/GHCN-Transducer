using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCoHarvester
{
    class GhcnQualifier
    {
        public GhcnQualifier(string code, string description)
        {
            QualifierCode = code;
            QualifierDescription = description;
        }

        public int QualifierID { get; set; }
        public string QualifierCode { get; set; }
        public string QualifierDescription { get; set; }
    }
}
