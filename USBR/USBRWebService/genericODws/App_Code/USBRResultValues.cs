using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace WaterOneFlow.odws
{
    /// <summary>
    /// Summary description for CDECResultsValues
    /// </summary>
    public class USBRResultsValues
    {

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Links
        {
            public string self { get; set; }
            public string first { get; set; }
            public string last { get; set; }           
            public string prev { get; set; } 
            public string next { get; set; }
        }
    

        public class Meta
        {
            public int totalItems { get; set; }
            public int itemsPerPage { get; set; }
            public int currentPage { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public int itemId { get; set; }
            public int locationId { get; set; }
            public string sourceCode { get; set; }
            public DateTime dateTime { get; set; }
            public string result { get; set; }
            public object status { get; set; }
            public object modelRunMemberId { get; set; }
            public object modelRunId { get; set; }
            public int parameterId { get; set; }
            public object resultAttributes { get; set; }
            public object lastUpdate { get; set; }
            public DateTime createDate { get; set; }
            public object updateDate { get; set; }
        }

        public class Data
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes attributes { get; set; }
        }

        public class USBRResultsValuesRoot
        {
            public Links links { get; set; }
            public Meta meta { get; set; }
            public List<Data> data { get; set; }
        }

    }
}