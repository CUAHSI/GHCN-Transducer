using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SCANHarvester
{
    class AwdbClient
    {
        private string _serviceUrl;

        public AwdbClient()
        {
            _serviceUrl = "https://wcc.sc.egov.usda.gov/awdbWebService/services";
        }

        public void GetSites()
        {
            // necessary to prevent the "Bad Gateway" error
            System.Net.ServicePointManager.Expect100Continue = false;

            var request = (HttpWebRequest)WebRequest.Create(_serviceUrl);

            string postData = 
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:q0=""http://www.wcc.nrcs.usda.gov/ns/awdbWebService"" xmlns:xsd = ""http://www.w3.org/2001/XMLSchema"" xmlns:xsi =""http://www.w3.org/2001/XMLSchema-instance"">
<SOAP-ENV:Body>
<q0:getStations>
<networkCds>SCAN</networkCds>
<logicalAnd>true</logicalAnd>
</q0:getStations>
</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

            var data = Encoding.UTF8.GetBytes(postData); // or UTF8

            request.Method = "POST";
            request.Accept = "*/*";
            request.UserAgent = "runscope/0.1";
            request.ContentLength = data.Length;

            var newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            // try sending the SOAP request ...
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var statusCode = (int)response.StatusCode;

                    if (statusCode == 200)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            var xmlDoc = XDocument.Load(reader);
                            var root = xmlDoc.Root;
                        }
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse resp = e.Response;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string msg = sr.ReadToEnd();
                    }
                }
            }
        }
    }
}
