using System;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NEONHarvester
{
    class TimeSeriesInfo
    {
        public string SiteCode { get; set; }
        public string VariableCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ValueCount { get; set; }
        public override string ToString()
        {
            return SiteCode + "|" + VariableCode;
        }
    }

    class DataValuesInfo
    {       
        public string SiteCode { get; set; }
        public string VariableCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Url { get; set; }
        public string Status { get; set; }
        public int ExpectedValueCount { get; set; }
        public int ActualValueCount { get; set; }
        public int TimeTakenSeconds { get; set; }
    }

    class WebServiceTester
    {
        private string _asmxUrl;

        public WebServiceTester(string asmxUrl)
        {
            _asmxUrl = asmxUrl;
        }

        public void Run()
        {
            var siteCodes = TestGetSites();
            Console.WriteLine(String.Format("testing {0} sites from url {1}...", siteCodes.Count, _asmxUrl));
            List<DataValuesInfo> testResultList = new List<DataValuesInfo>();
            int numSites = siteCodes.Count;
            int iSite = 0;
            foreach (var siteCode in siteCodes)
            {
                iSite += 1;
                var seriesList = TestGetSiteInfo(siteCode);

                int numSeriesAtSite = seriesList.Count;
                int iSeries = 0;
                foreach (TimeSeriesInfo seriesInfo in seriesList)
                {
                    iSeries += 1;
                    DataValuesInfo result = TestGetValues(seriesInfo);
                    Console.WriteLine(String.Format("site {0}/{1} {2}, series {3}/{4} {5}, time: {6}s, values: {7}, status: {8}",
                        iSite, numSites, result.SiteCode, iSeries, numSeriesAtSite, result.VariableCode,
                        result.TimeTakenSeconds, result.ActualValueCount, result.Status));
                    testResultList.Add(result);
                }
            }

            //TODO write result to csv or xlsx
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string reportFileName = exePath + "\\testing_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            using (StreamWriter file = new StreamWriter(reportFileName))
            {
                foreach (DataValuesInfo info in testResultList)
                {
                    var startDateStr = info.StartDate.ToString("s", CultureInfo.InvariantCulture);
                    var endDateStr = info.EndDate.ToString("s", CultureInfo.InvariantCulture);
                    var line = String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", 
                        info.SiteCode, info.VariableCode, startDateStr, endDateStr,
                        info.ExpectedValueCount, info.ActualValueCount, info.TimeTakenSeconds, info.Status);
                    // If the line doesn't contain the word 'Second', write the line to the file.
                    file.WriteLine(line);
                }
            }
        }

        public List<string> TestGetSites()
        {
            var siteCodeList = new List<String>();

            XmlDocument doc1 = new XmlDocument();
            var sitesUrl = _asmxUrl + "/GetSitesObject?site=&authToken=";
            doc1.Load(sitesUrl);
            XmlElement root = doc1.DocumentElement;

            foreach (XmlElement siteElement in root.ChildNodes)
            {
                if (siteElement.Name == "site")
                {
                    XmlElement siteInfo = (XmlElement) siteElement.FirstChild;
                    foreach(XmlElement siteProperty in siteInfo.ChildNodes)
                    {
                        if(siteProperty.Name == "siteCode")
                        {
                            var siteCodeValue = siteProperty.InnerText;
                            var siteNetwork = siteProperty.GetAttribute("network");
                            siteCodeList.Add(siteNetwork + ":" + siteCodeValue);
                            break;
                        }
                    }
                    XmlNode siteCode = siteInfo.SelectSingleNode("siteCode");
                }
            }

            return siteCodeList;
        }

        public List<TimeSeriesInfo> TestGetSiteInfo(string fullSiteCode)
        {
            var seriesList = new List<TimeSeriesInfo>();
            var siteInfoUrl = _asmxUrl + String.Format("/GetSiteInfoObject?site={0}&authToken=", fullSiteCode);
            XmlDocument doc1 = new XmlDocument();
            doc1.Load(siteInfoUrl);
            XmlElement root = doc1.DocumentElement;
            XmlElement site = (XmlElement) root.LastChild;
            XmlElement seriesCatalog = (XmlElement) root.LastChild.LastChild;
            foreach(XmlElement series in seriesCatalog.ChildNodes)
            {
                TimeSeriesInfo seriesInfo = new TimeSeriesInfo();
                foreach (XmlElement seriesProp in series.ChildNodes)
                {                    
                    if (seriesProp.Name == "variable")
                    {
                        foreach(XmlElement variableProp in seriesProp.ChildNodes)
                        {
                            if (variableProp.Name == "variableCode")
                            {
                                var variableCodeValue = variableProp.InnerText;
                                var vocab = variableProp.GetAttribute("vocabulary");
                                seriesInfo.VariableCode = vocab + ":" + variableCodeValue;
                                break;
                            }
                        }
                    }
                    else if (seriesProp.Name == "valueCount")
                    {
                        seriesInfo.ValueCount = Convert.ToInt32(seriesProp.InnerText);
                    }
                    else if (seriesProp.Name == "variableTimeInterval")
                    {
                        foreach(XmlElement timeProp in seriesProp.ChildNodes)
                        {
                            if (timeProp.Name == "beginDateTime")
                            {
                                seriesInfo.StartDate = Convert.ToDateTime(timeProp.InnerText);
                            }
                            else if (timeProp.Name == "endDateTime")
                            {
                                seriesInfo.EndDate = Convert.ToDateTime(timeProp.InnerText);
                            }
                        }
                    }
                }
                seriesInfo.SiteCode = fullSiteCode;
                seriesList.Add(seriesInfo);
            }
            return seriesList;
        }

        public DataValuesInfo TestGetValues(TimeSeriesInfo seriesInfo)
        {
            DataValuesInfo valuesInfo = new DataValuesInfo();
            valuesInfo.SiteCode = seriesInfo.SiteCode;
            valuesInfo.VariableCode = seriesInfo.VariableCode;
            valuesInfo.StartDate = seriesInfo.StartDate;
            valuesInfo.EndDate = seriesInfo.EndDate;

            var urlTemplate = "/GetValuesObject?location={0}&variable={1}&startDate={2}&endDate={3}&authToken=";
            var startDateStr = seriesInfo.StartDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            var endDateStr = seriesInfo.EndDate.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            var url = _asmxUrl + String.Format(urlTemplate, seriesInfo.SiteCode, seriesInfo.VariableCode, startDateStr, endDateStr);

            valuesInfo.Url = url;
            valuesInfo.ActualValueCount = 0;

            // start timing
            DateTime queryBeginTime = DateTime.Now;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(url);
                XmlElement root = doc.DocumentElement;
                XmlElement timeSeries = (XmlElement)root.LastChild;
                foreach (XmlElement seriesProp in timeSeries.ChildNodes)
                {
                    if (seriesProp.Name == "values")
                    {
                        if (seriesProp.HasChildNodes && seriesProp.FirstChild.Name == "value" && seriesProp.ChildNodes.Count > 5)
                        {
                            valuesInfo.ActualValueCount = seriesProp.ChildNodes.Count;
                            valuesInfo.Status = "ok";
                        }
                        else
                        {
                            valuesInfo.Status = "no data values in response";
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                valuesInfo.Status = "WOF ERROR: " + ex.Message;
            }
            int timeTakenSeconds = Convert.ToInt32(Math.Round((DateTime.Now - queryBeginTime).TotalSeconds));
            valuesInfo.TimeTakenSeconds = timeTakenSeconds; 

            return valuesInfo;
        }
    }
}
