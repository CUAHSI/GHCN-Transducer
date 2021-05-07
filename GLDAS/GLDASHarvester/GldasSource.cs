using System;

namespace GldasHarvester
{
    class GldasSource
    {
        public int SourceID { get; set; }
        public string Organization { get; set; }
        public string SourceDescription { get; set; }
        public string SourceLink { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Citation { get; set; }
        public int MetadataID { get; set; }
        public string SourceCode { get; set; }
    }
}
