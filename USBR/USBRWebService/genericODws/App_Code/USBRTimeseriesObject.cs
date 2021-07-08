using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for USBRTimeseriesObject
/// </summary>
public class USBRTimeseriesObject
{
    public USBRTimeseriesObject()
    {
        //
        // TODO: Add constructor logic here
        //
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Coordinates
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class Location
    {
        public string Name { get; set; }
        public string State { get; set; }
        public Coordinates Coordinates { get; set; }
    }

    public class Value
    {
        public string dateTime { get; set; }
        public string result { get; set; }
        public string timeStep { get; set; }
        public string resultType { get; set; }
    }

    public List<Value> data { get; set; }

}