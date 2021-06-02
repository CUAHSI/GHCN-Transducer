using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static USBRHarvester.USBRCatalogItem;
using static USBRHarvester.USBRLocationPoint;
using static USBRHarvester.USBRParameter;

namespace USBRHarvester
{
    class RISE_ApiManager
    {
        private LogWriter _log;
        private static string _USBRAPIurl = "https://data.usbr.gov/rise/api/";

        public RISE_ApiManager(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
        }

        public async Task<List<USBRCatalogRecord.Data>> GetCatalogRecordsAsync(Action<USBRCatalogRecord.USBRCatalogRecordRoot> callBack = null)
        {
            //GetCatalogItems from API that are timeseries "itemstructureId=1, Geospatial=2, FileUpload=3" max per page = 100
            //TODO:iterate through pages
            var catalogRecords = new List<USBRCatalogRecord.Data>();
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_USBRAPIurl);
            var nextUrl = "catalog-record?itemStructureId=1&hasItemStructure=true&itemsPerPage=100";

            do
            {
                await httpClient.GetAsync(nextUrl)
                    .ContinueWith(async (recordSearchTask) =>
                    {
                        var response = await recordSearchTask;
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonString = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<USBRCatalogRecord.USBRCatalogRecordRoot>(jsonString);
                            if (result != null)
                            {
                                // Build the full list to return later after the loop.
                                if (result.data.Any())
                                    catalogRecords.AddRange(result.data.ToList());

                                // Run the callback method, passing the current page of data from the API.
                                //if (callBack != null)
                                //        callBack(result);

                                // Get the URL for the next page
                                nextUrl = (result.links.next != null) ? result.links.next : string.Empty;
                            }
                        }
                        else
                        {
                        // End loop if we get an error response.
                        nextUrl = string.Empty;
                        }
                    });

            } while (!string.IsNullOrEmpty(nextUrl));
            return catalogRecords;           

        }

        public async Task<(List<USBRCatalogItem.Data>, List<USBRLocationPointRoot>)> GetCatalogItemsAsync(List<USBRCatalogRecord.Data> catalogRecords)
       
        {
            //GetCatalogItems from API that are timeseries "itemstructureId=1, Geospatial=2, FileUpload=3" max per page = 100
            //TODO:iterate through pages
            var catalogItems = new List<USBRCatalogItem.Data>();
            var locations = new List<USBRLocationPointRoot>();
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_USBRAPIurl);
            //var nextUrl = "catalog-item?id="+ catalogRecords[0].id + "&hasItemStructure=true&itemsPerPage=1";
            //truncate list for testing
            //catalogRecords.RemoveRange(200, 284);
            //catalogRecords.Where(p => p.relationships.location.data.id != "398");
            //foreach (var record in catalogRecords.Where(p => p.relationships.location.data.id == "/rise/api/location/398"))

            foreach (var record in catalogRecords)
            {
                await httpClient.GetAsync("location/" + record.relationships.location.data.id.Split('/').Last())
                        .ContinueWith(async (itemSearchTask) =>
                        {
                            var response = await itemSearchTask;
                            if (response.IsSuccessStatusCode)
                            {
                                string jsonString = await response.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<USBRLocationPointRoot>(jsonString);
                                if (result != null)
                                {
                                    // Build the full list to return later after the loop.
                                    if (result.data.attributes.locationCoordinates.type == "Point")
                                    { 
                                        if (!locations.Exists(r=> r.data.id == result.data.id))
                                        
                                        locations.Add(result);
                                        _log.LogWrite(result.data.attributes._id + "," + result.data.attributes.locationName + ", " + result.data.attributes.horizontalDatum._id + ", " + (result.data.attributes.verticalDatum != null ? result.data.attributes.verticalDatum._id : "null") + ", " + (result.data.attributes.elevation != null ? result.data.attributes.elevation : DBNull.Value.ToString()));

                                        
                                         
                                    }
                                    // Run the callback method, passing the current page of data from the API.
                                    //if (callBack != null)
                                    //        callBack(result);

                                    // Get the URL for the next page
                                    //nextUrl = (result.links.next != null) ? result.links.next : string.Empty;
                                }
                            }
                            else
                            {
                                // End loop if we get an error response.
                                //nextUrl = string.Empty;
                            }
                        });

                foreach (var item in record.relationships.catalogItems.data.ToList())
                {
                    await httpClient.GetAsync("catalog-item?id=" + item.id)
                        .ContinueWith(async (itemSearchTask) =>
                        {
                            var response = await itemSearchTask;
                            if (response.IsSuccessStatusCode)
                            {
                                string jsonString = await response.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<USBRCatalogItem.USBRCatalogitemRoot>(jsonString);
                                if (result != null)
                                {
                                    // Build the full list to return later after the loop.
                                    if (result.data.Any())
                                        catalogItems.AddRange(result.data.ToList());

                                    // Run the callback method, passing the current page of data from the API.
                                    //if (callBack != null)
                                    //        callBack(result);

                                    // Get the URL for the next page
                                    //nextUrl = (result.links.next != null) ? result.links.next : string.Empty;
                                }
                            }
                            else
                            {
                                // End loop if we get an error response.
                                //nextUrl = string.Empty;
                            }
                        });
                }
            } 
            return (catalogItems, locations);

        }

        public async Task<List<USBRParameter.Data>> GetParametersAsync(Action<USBRParameterRoot> callBack = null)
        {
            //GetCatalogItems from API that are timeseries "itemstructureId=1, Geospatial=2, FileUpload=3" max per page = 100
            //TODO:iterate through pages
            var parameters = new List<USBRParameter.Data>();
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_USBRAPIurl);
            var nextUrl = "parameter";

            do
            {
                await httpClient.GetAsync(nextUrl)
                    .ContinueWith(async (recordSearchTask) =>
                    {
                        var response = await recordSearchTask;
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonString = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<USBRParameterRoot>(jsonString);
                            if (result != null)
                            {
                                // Build the full list to return later after the loop.
                                if (result.data.Any())
                                    parameters.AddRange(result.data.ToList());

                                // Run the callback method, passing the current page of data from the API.
                                //if (callBack != null)
                                //        callBack(result);

                                // Get the URL for the next page
                                nextUrl = (result.links.next != null) ? result.links.next : string.Empty;
                            }
                        }
                        else
                        {
                            // End loop if we get an error response.
                            nextUrl = string.Empty;
                        }
                    });

            } while (!string.IsNullOrEmpty(nextUrl));
            return parameters;

        }

        public void GetParameters()
        {
            //GetCatalogItems from API that are timeseries "itemstructureId=1, Geospatial=2, FileUpload=3" max per page = 100
            //TODO:iterate through pages
            
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = _USBRAPIurl;
                    var jsonData = webClient.DownloadString("parameter");
                    //var USBRParameters = JsonConvert.DeserializeObject<USBRParameters.Data>(jsonData);
                    //_log.LogWrite(String.Format("Found {0} distinct USBRCatalogRecord.", USBRParameters..Count));
                }
            }
            catch (WebException ex)
            {
                throw ex;
            }


            //try
            //{
            //    using (var stream = client.OpenRead(_USBRAPIurl + "catalog-record?itemStructureId=1&hasItemStructure=true&itemsPerPage=100"))
            //    using (var reader = new StreamReader(stream))
            //    {
            //        string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            //        using (SqlConnection connection = new SqlConnection(connString))
            //        {
            //            // to remove any old ("unused") variables
            //            //DeleteOldVariables(connection);

            //            foreach (var item in USBRcatalogRecords.data)
            //            {
            //                try
            //                {
            //                    //object variableID = SaveOrUpdateVariable(item, connection);
            //                }
            //                catch (Exception ex)
            //                {
            //                    _log.LogWrite("error updating variable: " + item + " " + ex.Message);
            //                }

            //            }
            //        }
            //        _log.LogWrite("UpdateVariables OK: " + USBRcatalogRecords.data.Count.ToString() + " variables.");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _log.LogWrite("UpdateVariables ERROR on row: " + " " + ex.Message);
            //}
        }


        public void ResultCallBack(USBRCatalogRecord.USBRCatalogRecordRoot jobSearchResult)
        {
            if (jobSearchResult != null && jobSearchResult.data.Count > 0)
            {
               // Console.WriteLine($"\nDisplaying jobs {jobSearchResult.data..firstDocument} to {jobSearchResult.lastDocument}");
                foreach (var item in jobSearchResult.data)
                {
                    Console.WriteLine(item.attributes.tags);
                    Console.WriteLine(item.id);
                }
            }
        }

        
    }
}

    
