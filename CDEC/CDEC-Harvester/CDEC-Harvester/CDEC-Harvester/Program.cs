using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CDEC_Harvester
{
    public class Program
    {
            
        static void Main(string[] args)
        {
            //var sandbox = new sandbox();
            //sandbox.test();
            //return;


            // (0) setup progress logging with writing to console and log file
            var logger = new LogWriter(true);

            //read Stations csv contains site and sensor info manually retrieved from 
            //var stations = ReadStationCSV(logger);

            // (1) updating sites
            var siteM = new SiteManager(logger);
            var stationsIds = siteM.getStationList();

            //limit size while debugging
            //stationsIds.RemoveRange(0, 2800);//Where(p => p.id.Contains("4371")).ToList();
            //stationsIds = stationsIds.Where(p => p.Contains("PMD")).ToList();
          
            var stations = siteM.getMetadatafromStationPage(stationsIds);          

            //serialize list for debugging 
            //string json = JsonConvert.SerializeObject(stations.ToArray());
            //System.IO.File.WriteAllText(@"C:\Temp\CDECSTations.json", json);

            siteM.UpdateSites_fast(stations);

            // (2) updating variables
            var varM = new VariableManager(logger);
            varM.UpdateVariables();

            // (3) updating methods
            //var methM = new MethodManager(logger);
            //methM.UpdateMethods();

            // (4) updating sources
            var srcM = new SourceManager(logger);
            srcM.UpdateSources();

            // (5) updating the series catalog
            SeriesCatalogManager seriesM = new SeriesCatalogManager(logger);
            seriesM.UpdateSeriesCatalog_fast(stations);


        }

        private static List<StationsModel> ReadStationCSV(LogWriter logger)
        {
            //set filename
            string fileName = "CDEC_Stations.csv";
            //Get path to file
            string path = AppDomain.CurrentDomain.BaseDirectory + "/Files/" + fileName;
            //init station list
            var stations = new List<StationsModel>();

            try
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<StationsModelClassMap>();
                    stations = csv.GetRecords<StationsModel>().ToList();
                }
               
            }
            catch (Exception ex)
            {
                logger.LogWrite("Error: read Stations csv: " + ex.Message);
            } 
            return stations;
        }


    }
}
