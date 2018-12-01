using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace NEONHarvester
{
    class NeonApiReader
    {
        private LogWriter _log;

        public NeonApiReader(LogWriter log)
        {
            _log = log;
            NeonApiUrl = "http://data.neonscience.org/api/v0";
        }

        public string NeonApiUrl { get; }


        public NeonProduct ReadProductFromApi(string productCode)
        {
            var neonProduct = new NeonProduct();
            string url = NeonApiUrl + "/products/" + productCode;

            try
            {
                
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    var jsonData = client.DownloadString(url);

                    var neonProductData = JsonConvert.DeserializeObject<NeonProductData>(jsonData);
                    neonProduct = neonProductData.data;
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite(string.Format("ReadProductFromApi ERROR for url {0}: ", url) + ex.Message);
            }
            return (neonProduct);
        }


        public NeonProductCollection ReadProductsFromApi()
        {
            var neonProducts = new NeonProductCollection();
            try
            {
                string url = NeonApiUrl + "/products/";
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    var jsonData = client.DownloadString(url);

                    neonProducts = JsonConvert.DeserializeObject<NeonProductCollection>(jsonData);
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadProductsFromApi ERROR: " + ex.Message);
            }
            return (neonProducts);
        }


        public NeonFileCollection ReadNeonFilesFromApi(string filesUrl)
        {
            var neonFiles = new NeonFileCollection();

            var client = new WebClient();
            using (var stream = client.OpenRead(filesUrl))
            using (var reader = new StreamReader(stream))
            {
                var jsonData = client.DownloadString(filesUrl);

                var neonFileData = JsonConvert.DeserializeObject<NeonFileData>(jsonData);
                neonFiles = neonFileData.data;
            }
            return (neonFiles);
        }


        public NeonSiteCollection ReadSitesFromApi()
        {
            var neonSites = new NeonSiteCollection();
            try
            {
                string url = NeonApiUrl + "/sites/";
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    var jsonData = client.DownloadString(url);

                    neonSites = JsonConvert.DeserializeObject<NeonSiteCollection>(jsonData);
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSitesFromApi ERROR: " + ex.Message);
            }
            return (neonSites);
        }


        public NeonSite ReadSiteFromApi(string neonSiteCode)
        {
            var neonSite = new NeonSite();
            try
            {
                string url = NeonApiUrl + "/sites/" + neonSiteCode;
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    var jsonData = client.DownloadString(url);

                    NeonSiteItem siteData = JsonConvert.DeserializeObject<NeonSiteItem>(jsonData);
                    return siteData.data;
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSitesFromApi ERROR for site " + neonSiteCode + ": " + ex.Message);
            }
            return (null);
            
        }
    }
}
