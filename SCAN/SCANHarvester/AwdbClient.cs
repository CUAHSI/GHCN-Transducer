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
        private LogWriter _logger;

        public AwdbClient(LogWriter logger)
        {
            _serviceUrl = "https://wcc.sc.egov.usda.gov/awdbWebService/services";
            _logger = logger;

            // !!! necessary configuration to prevent the "Bad Gateway" error
            // see https://www.wcc.nrcs.usda.gov/web_service/awdb_web_service_faq.htm
            System.Net.ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Converts value in feet to value in meters (for elevation)
        /// </summary>
        /// <param name="valueInFeet">The elevation value in ft</param>
        /// <returns>The elevation value in m</returns>
        private decimal ft2m(decimal valueInFeet)
        {
            return valueInFeet * 0.3048m;
        }

        private string fips2stateName(string fipsStateNumber)
        {
            switch (fipsStateNumber)
            {
                case "01":
                    return "Alabama";
                case "02":
                    return "Alaska";
                case "04":
                    return "Arizona";
                case "05":
                    return "Arkansas";
                case "06":
                    return "California";
                case "08":
                    return "Colorado";
                case "09":
                    return "Connecticut";
                case "10":
                    return "Delaware";
                case "11":
                    return "District of Columbia";
                case "12":
                    return "Florida";
                case "13":
                    return "Georgia";
                case "15":
                    return "Hawaii";
                case "16":
                    return "Idaho";
                case "17":
                    return "Illinois";
                case "18":
                    return "Indiana";
                case "20":
                    return "Kansas";
                case "21":
                    return "Kentucky";
                case "22":
                    return "Louisiana";
                case "25":
                    return "Massachusetts";
                case "24":
                    return "Maryland";
                case "23":
                    return "Maine";
                case "26":
                    return "Michigan";
                case "27":
                    return "Minnesota";
                case "28":
                    return "Mississippi";
                case "29":
                    return "Missouri";
                case "30":
                    return "Montana";
                case "31":
                    return "Nebraska";
                case "32":
                    return "Nevada";
                case "33":
                    return "New Hampshire";
                case "34":
                    return "New Jersey";
                case "35":
                    return "New Mexico";
                case "36":
                    return "New York";
                case "37":
                    return "North Carolina";
                case "38":
                    return "North Dakota";
                case "39":
                    return "Ohio";
                case "40":
                    return "Oklahoma";
                case "41":
                    return "Oregon";
                case "42":
                    return "Pennsylvania";
                case "44":
                    return "Rhode Island";
                case "45":
                    return "South Carolina";
                case "46":
                    return "South Dakota";
                case "47":
                    return "Tennessee";
                case "48":
                    return "Texas";
                case "49":
                    return "Utah";
                case "50":
                    return "Vermont";
                case "51":
                    return "Virginia";
                case "53":
                    return "Washington";
                case "54":
                    return "West Virginia";
                case "55":
                    return "Wisconsin";
                case "56":
                    return "Wyoming";
                case "72":
                    return "Puerto Rico";
                default:
                    return fipsStateNumber;
            }
        }

        public string[] ListUniqueElements()
        {
            var uniqueElements = new HashSet<string>();

            // using auto-generated class from AWDB SOAP service reference
            var wsc = new Awdb.AwdbWebServiceClient();

            // download a list of all SCAN station triplets
            string[] stationTriplets = LoadStationTriplets(wsc);

            foreach (string stationTriplet in stationTriplets)
            {
                var elements = wsc.getStationElements(stationTriplet, null, null);

                // ignore stations with no elements
                if (elements is null) { continue; }

                // add elements at this station to unique list
                foreach (var element in elements)
                {
                    string elementName = element.elementCd;
                    if (!uniqueElements.Contains(elementName))
                    {
                        uniqueElements.Add(elementName);
                    }
                }
            }
            return uniqueElements.ToArray();
        }


        private string GetCuahsiVariableCode(string elementCd, string duration, decimal heightDepth, string heightDepthUnits)
        {
            string durationCode;
            string heightDepthUnitsCode = "";
            if (heightDepthUnits != null)
            {
                heightDepthUnitsCode = heightDepthUnits;
            }

            switch (duration)
            {
                case "INSTANTANEOUS":
                    durationCode = "i";
                    break;
                case "HOURLY":
                    durationCode = "H";
                    break;
                case "DAILY":
                    durationCode = "D";
                    break;
                case "SEMIMONTHLY":
                    durationCode = "sm";
                    break;
                case "MONTHLY":
                    durationCode = "m";
                    break;
                case "SEASONAL":
                    durationCode = "season";
                    break;
                case "WATER_YEAR":
                    durationCode = "wy";
                    break;
                case "CALENDAR_YEAR":
                    durationCode = "y";
                    break;
                default:
                    durationCode = "unknown";
                    break;
            }

            if (heightDepth != 0)
            {
                string depthCode;
                if (heightDepth < 0)
                {
                    depthCode = "D" + (Math.Abs(heightDepth)).ToString() + heightDepthUnitsCode;
                }
                else
                {
                    depthCode = "H" + (Math.Abs(heightDepth)).ToString() + heightDepthUnitsCode;
                }
                return String.Format("{0}_{1}_{2}", elementCd, durationCode, depthCode);
            }
            else
            {
                return String.Format("{0}_{1}", elementCd, durationCode);
            }
        }


        public string[] ListUniqueVariables()
        {
            var uniqueElements = new HashSet<string>();

            // using auto-generated class from AWDB SOAP service reference
            var wsc = new Awdb.AwdbWebServiceClient();

            // download a list of all SCAN station triplets
            string[] stationTriplets = LoadStationTriplets(wsc);

            foreach (string stationTriplet in stationTriplets)
            {
                var elements = wsc.getStationElements(stationTriplet, null, null);

                // ignore stations with no elements
                if (elements is null) { continue; }

                // unique variable code is a combination of code, duration and heightDepth
                foreach (var element in elements)
                {
                    string elementCd = element.elementCd;
                    string duration = element.duration.ToString();
                    var hd = element.heightDepth;
                    decimal heightDepthValue = 0.0M;
                    string heightDepthUnits = null;
                    if (hd != null)
                    {
                        heightDepthValue = hd.value;
                        heightDepthUnits = hd.unitCd;
                    }
                    string uniqueCode = GetCuahsiVariableCode(elementCd, duration, heightDepthValue, heightDepthUnits);

                    if (!uniqueElements.Contains(uniqueCode))
                    {
                        uniqueElements.Add(uniqueCode);
                    }
                }
            }


            // saving outputs to a .csv file
            using (var file = File.CreateText("variables.csv"))
            {
                foreach (var arr in uniqueElements)
                {
                    //file.WriteLine(string.Join(",", arr));
                    file.WriteLine(arr);
                }
            }
            return uniqueElements.ToArray();
        }

        private string[] LoadStationTriplets(Awdb.AwdbWebServiceClient wsc)
        {
            var stationIds = new string[] { };
            var stateCds = new string[] { };
            var networkCds = new string[] { "SCAN" };
            var hucs = new string[] { };
            var countyNames = new string[] { };
            var minLatitude = -90;
            var maxLatitude = 90;
            var minLongitude = -180;
            var maxLongitude = 180;
            var minElevation = -100000;
            var maxElevation = 100000;
            var elementCds = new string[] { };
            var ordinals = new int[] { };
            var heightDepths = new Awdb.heightDepth[] { };
            var logicalAnd = true;

            var result = wsc.getStations(stationIds, stateCds, networkCds, hucs, countyNames,
                minLatitude, maxLatitude, minLongitude, maxLongitude, minElevation, maxElevation,
                elementCds, ordinals, heightDepths, logicalAnd);

            return result;
        }

        public List<Series> GetAllSeries()
        {
            List<Series> seriesList = new List<Series>();

            var wsc = new Awdb.AwdbWebServiceClient();
            string[] stationTriplets = LoadStationTriplets(wsc);
            var stationCount = stationTriplets.Length;
            _logger.LogWrite("Retrieving series catalog for " + stationTriplets.Length.ToString() + " stations.");

            foreach(string stationTriplet in stationTriplets)
            {
                var elements = wsc.getStationElements(stationTriplet, null, null);

                // ignore stations with no elements
                if (elements is null) { continue; }

                // element to series
                foreach (var element in elements)
                {
                    Series series = new Series();

                    string elementCd = element.elementCd;
                    string duration = element.duration.ToString();
                    var hd = element.heightDepth;
                    decimal heightDepthValue = 0.0M;
                    string heightDepthUnits = null;
                    if (hd != null)
                    {
                        heightDepthValue = hd.value;
                    }
                    // get the variable code
                    string variableCode = GetCuahsiVariableCode(elementCd, duration, heightDepthValue, heightDepthUnits);

                    // get the method code ---> from variable code ...
                    // extract the method code from the name
                    string[] codeSplit = variableCode.Split('_');
                    string methodCode = "NOTSPECIFIED";
                    if (codeSplit.Length > 2)
                    {
                        methodCode = codeSplit[2];
                    }
                    else
                    {
                        methodCode = "NOTSPECIFIED";
                    }
                    series.MethodCode = methodCode;
                    series.SiteCode = stationTriplet.Replace(":", "_");
                    series.VariableCode = variableCode;
                    series.BeginDateTime = Convert.ToDateTime(element.beginDate);
                    series.EndDateTime = Convert.ToDateTime(element.endDate);
                    series.BeginDateTimeUTC = series.BeginDateTime;
                    series.EndDateTimeUTC = series.EndDateTime;

                    // estimate the value count
                    DateTime realEndTime = series.EndDateTime;
                    if (series.EndDateTime > DateTime.Now.AddDays(1))
                    {
                        realEndTime = DateTime.Now.AddDays(1);
                    }
                    double estValueCount = (realEndTime - series.BeginDateTime).TotalHours;
                    switch (element.duration)
                    {
                        case Awdb.duration.ANNUAL:
                        case Awdb.duration.CALENDAR_YEAR:
                        case Awdb.duration.WATER_YEAR:
                            estValueCount = ((realEndTime - series.BeginDateTime).TotalDays) / 365.0;
                            break;
                        case Awdb.duration.SEASONAL:
                            estValueCount = ((realEndTime - series.BeginDateTime).TotalDays) / 90.0;
                            break;
                        case Awdb.duration.MONTHLY:
                            estValueCount = ((realEndTime - series.BeginDateTime).TotalDays) / 30.5;
                            break;
                        case Awdb.duration.SEMIMONTHLY:
                            estValueCount = ((realEndTime - series.BeginDateTime).TotalDays) / 15.0;
                            break;
                        case Awdb.duration.DAILY:
                            estValueCount = (realEndTime - series.BeginDateTime).TotalDays;
                            break;
                        case Awdb.duration.HOURLY:
                            estValueCount = (realEndTime - series.BeginDateTime).TotalHours;
                            break;

                    }
                    series.ValueCount = (int)(Math.Ceiling(estValueCount));
                    seriesList.Add(series);
                }
            }

            return seriesList;

        }

        public List<Site> GetStations()
        {
            // !!! necessary to prevent the "Bad Gateway" error (moved to class constructor)
            //System.Net.ServicePointManager.Expect100Continue = false;
            List<Site> siteList = new List<Site>();

            // using auto-generated class from AWDB SOAP service reference
            var wsc = new Awdb.AwdbWebServiceClient();

            // download a list of all SCAN station triplets
            string[] stationTriplets = LoadStationTriplets(wsc);
            var stationCount = stationTriplets.Length;
            _logger.LogWrite("Retrieved SCAN Station codes from AWDB service: found " + stationTriplets.Length.ToString() + " stations.");
            _logger.LogWrite("Searching SCAN Station metadata ... ");

            foreach (string stationTriplet in stationTriplets)
            {
                // call method "getStationMetadata"
                var metadata = wsc.getStationMetadata(stationTriplet);

                var site = new Site
                {
                    SiteCode = metadata.stationTriplet.Replace(":", "_"),
                    SiteName = metadata.name,
                    Latitude = metadata.latitude,
                    Longitude = metadata.longitude,
                    Elevation = ft2m(metadata.elevation),
                    State = fips2stateName(metadata.fipsStateNumber),
                    County = metadata.countyName,
                    HUC = metadata.huc,
                    HUD = metadata.hud,
                    ActonId = metadata.actonId,
                    ShefId = metadata.shefId,
                    BeginDate = Convert.ToDateTime(metadata.beginDate),
                    EndDate = Convert.ToDateTime(metadata.endDate),
                    TimeZone = metadata.stationDataTimeZone,
                    StationTriplet = metadata.stationTriplet
                };
                siteList.Add(site);

            }

            return siteList;

        }
    }
}