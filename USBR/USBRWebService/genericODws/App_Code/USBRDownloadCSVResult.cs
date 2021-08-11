using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for USBRDownloadCSVResult
/// </summary>

public class USBRDownloadCSVResult
{
    public string Location { get; set; }
    public string Parameter { get; set; }
    public string Result { get; set; }
    public string Units { get; set; }
    public string Timestep { get; set; }
    public string Aggregation { get; set; }
    public DateTime DatetimeUTC { get; set; }
}

public class USBRDownloadCSVResultClassMap : ClassMap<USBRDownloadCSVResult>
{
    public USBRDownloadCSVResultClassMap()
    {
        Map(m => m.Location).Index(0);
        Map(m => m.Parameter).Index(1);
        Map(m => m.Result).Index(2);
        Map(m => m.Units).Index(3);
        Map(m => m.Timestep).Index(4);
        Map(m => m.Aggregation).Index(5);
        Map(m => m.DatetimeUTC).Index(6);
    }
}
