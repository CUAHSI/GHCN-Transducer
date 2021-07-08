using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for CDECValueResponse
/// </summary>
   

public class CDECValueResponseModel
{
    public string STATION_ID { get; set; }
    public string DURATION { get; set; }
    public int SENSOR_NUMBER { get; set; }
    public string SENSOR_TYPE { get; set; }
    public string DATETIME { get; set; }
    public string OBSDATE { get; set; }
    public double VALUE { get; set; }
    public string DATA_FLAG { get; set; }
    public string UNITS { get; set; }
}

public class CDECValueResponseModelClassMap : ClassMap<CDECValueResponseModel>
{
    public CDECValueResponseModelClassMap()
    {
        Map(m => m.STATION_ID).Name("STATION_ID");
        Map(m => m.DURATION).Name("DURATION");
        Map(m => m.SENSOR_NUMBER).Name("SENSOR_NUMBER");
        Map(m => m.SENSOR_TYPE).Name("SENSOR_TYPE");
        Map(m => m.DATETIME).Name("DATE TIME");
        Map(m => m.OBSDATE).Name("OBS DATE");
        Map(m => m.VALUE).Name("VALUE");
        Map(m => m.DATA_FLAG).Name("DATA_FLAG");
        Map(m => m.UNITS).Name("UNITS");
    }
}
