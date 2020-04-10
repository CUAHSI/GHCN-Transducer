using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Web;
using System.Configuration;
using WaterOneFlow.Schema.v1_1;
using WaterOneFlow;
using WaterOneFlowImpl.v1_1;
using WaterOneFlowImpl;
using System.Data;
using WaterOneFlowImpl.geom;
using System.Net;
using System.IO;
using System.Linq;
using System.Globalization;

namespace WaterOneFlow.odws
{
    /// <summary>
    /// The web service utilities
    /// </summary>
    public class GetValuesCoCo
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["ODDB"].ConnectionString;
        }

        public static string GetBaseUri()
        {
            string uri = HttpContext.Current.Request.Url.ToString();
            if (uri.EndsWith(".asmx"))
            {
                return uri;
            }
            else
            {
                return uri.Remove(uri.IndexOf(".asmx")) + ".asmx";
            }
        }

        public static QueryInfoType CreateQueryInfo(string webMethodName)
        {
            QueryInfoType queryInfo = new QueryInfoType();
            queryInfo.creationTime = DateTime.Now;
            queryInfo.creationTimeSpecified = true;
            queryInfo.criteria = new QueryInfoTypeCriteria();
            queryInfo.criteria.locationParam = String.Empty;
            queryInfo.criteria.MethodCalled = webMethodName;
            queryInfo.criteria.variableParam = String.Empty;
            queryInfo.queryURL = @"http://example.com";

            return queryInfo;
        }

        /// <summary>
        /// GetValues custom implementation
        /// </summary>
        /// <param name="SiteNumber">network:SiteCode</param>
        /// <param name="Variable">vocabulary:VariableCode</param>
        /// <param name="StartDate">yyyy-MM-dd</param>
        /// <param name="EndDate">yyyy-MM-dd</param>
        /// <returns></returns>
        public static TimeSeriesResponseType GetValues(string SiteNumber, string Variable, string StartDate, string EndDate)
        {
            //get siteId, variableId
            string siteId = SiteNumber.Substring(SiteNumber.LastIndexOf(":") + 1);
            string variableId = Variable.Substring(Variable.LastIndexOf(":") + 1);

            DateTime startDateTime = new DateTime(2000, 1, 1);
            DateTime endDateTime = DateTime.Now;

            if (StartDate != String.Empty)
            {
                startDateTime = DateTime.Parse(StartDate);
            }

            if (EndDate != String.Empty)
            {
                endDateTime = DateTime.Parse(EndDate);
            }

            TimeSeriesResponseType resp = new TimeSeriesResponseType();
            //resp.timeSeries[0].values[0].
            resp.timeSeries = new TimeSeriesType[1];
            resp.timeSeries[0] = new TimeSeriesType();
            resp.timeSeries[0].sourceInfo = GetSiteFromDb2(siteId);
            resp.timeSeries[0].variable = GetVariableInfoFromDb(variableId);

            resp.timeSeries[0].values = new TsValuesSingleVariableType[1];

            resp.timeSeries[0].values[0] = GetValuesFromDb(siteId, variableId, startDateTime, endDateTime);

            //set the query info
            resp.queryInfo = new QueryInfoType();
            resp.queryInfo.criteria = new QueryInfoTypeCriteria();

            resp.queryInfo.creationTime = DateTime.UtcNow;
            resp.queryInfo.creationTimeSpecified = true;
            resp.queryInfo.criteria.locationParam = SiteNumber;
            resp.queryInfo.criteria.variableParam = Variable;
            resp.queryInfo.criteria.timeParam = CuahsiBuilder.createQueryInfoTimeCriteria(StartDate, EndDate);

            return resp;

        }


        public static int VariableCodeToID(string variableCode)
        {
            string prefix = ConfigurationManager.AppSettings["vocabulary"];
            string varCode = variableCode;
            if (variableCode.StartsWith(prefix))
            {
                varCode = varCode.Substring(prefix.Length + 1);
            }
            switch (varCode)
            {
                case "SRAZKY":
                    return 1;
                case "VODSTAV":
                    return 4;
                case "PRUTOK":
                    return 5;
                case "SNIH":
                    return 8;
                case "TEPLOTA":
                    return 16;
                case "TMIN":
                    return 17;
                case "TMAX":
                    return 18;
                default:
                    return 0;
            }
        }


        /// <summary>
        /// Get the values, from the online .dly file
        /// </summary>
        /// <param name="siteCode">site code (local database id)</param>
        /// <param name="variableId">variable id (local database id)</param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        internal static TsValuesSingleVariableType GetValuesFromDb(string siteCode, string variableCode, DateTime startDateTime, DateTime endDateTime)
        {


            //default methodID and qcID
            int methodID = 0;
            int qcID = 1;
            int sourceID = 1;

            //numeric variable id
            int varId = VariableCodeToID(variableCode);

            //to get values, from the db
            TsValuesSingleVariableType s = new TsValuesSingleVariableType();
            s.censorCode = new CensorCodeType[1];
            s.censorCode[0] = new CensorCodeType();
            s.censorCode[0].censorCode = "nc";
            s.censorCode[0].censorCodeDescription = "not censored";
            s.censorCode[0].censorCodeID = 1;
            s.censorCode[0].censorCodeIDSpecified = true;

            //method
            s.method = new MethodType[1];
            s.method[0] = GetMethodFromDb(0);

            //variable
            VariableInfoType v = GetVariableInfoFromDb(variableCode);

            //time units
            s.units = v.timeScale.unit;

            //qc level
            s.qualityControlLevel = new QualityControlLevelType[1];
            s.qualityControlLevel[0] = GetQualityControlFromDb(qcID);
            s.qualityControlLevel[0].qualityControlLevelIDSpecified = true;

            //source
            s.source = new SourceType[1];
            s.source[0] = GetSourceFromDb();

            //qualifiers
            s.qualifier = new QualifierType[3];
            s.qualifier[0] = new QualifierType();
            Dictionary<string, QualifierType> allQualifiers = GetQualifiersFromDb();
            var usedQualifiers = new Dictionary<string, QualifierType>();

            //values: get from.dly files
            string dlyFileUrl = "https://www1.ncdc.noaa.gov/pub/data/ghcn/daily/all/" + siteCode + ".dly";

            //text file column positions
            TextFileColumn yearPos = new TextFileColumn(12, 15);
            TextFileColumn monthPos = new TextFileColumn(16, 17);
            TextFileColumn varcodePos = new TextFileColumn(18, 21);

            int[] valueCols = new int[31];
            int valueChars = 5;
            for (int i = 0; i < 31; i++)
            {
                valueCols[i] = 21 + i * 8;
            }
            var mflagCols = new int[31];
            for (int i = 0; i < 31; i++)
            {
                mflagCols[i] = 26 + i * 8;
            }
            int[] qflagCols = new int[31];
            for (int i = 0; i < 31; i++)
            {
                qflagCols[i] = 27 + i * 8;
            }
            int[] sflagCols = new int[31];
            for (int i = 0; i < 31; i++)
            {
                sflagCols[i] = 28 + i * 8;
            }
            List<ValueSingleVariable> valuesList = new List<ValueSingleVariable>();


            var client = new WebClient();
            using (var stream = client.OpenRead(dlyFileUrl))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string varCode = line.Substring(varcodePos.Start, varcodePos.Length);

                    // next continue only if varCode is same as in the query..
                    if (varCode == variableCode)
                    {
                        int year = Convert.ToInt32(line.Substring(yearPos.Start, yearPos.Length));
                        int month = Convert.ToInt32(line.Substring(monthPos.Start, monthPos.Length));

                        // for each value in month: add the value
                        int daysInMonth = DateTime.DaysInMonth(year, month);
                        DateTime firstDayInMonth = new DateTime(year, month, 1);
                        for (int d = 0; d < daysInMonth; d++)
                        {
                            DateTime obsDate = firstDayInMonth.AddDays(d);

                            // continue if date is between startTime, endTime
                            if (obsDate >= startDateTime && obsDate <= endDateTime)
                            {
                                ValueSingleVariable val = new ValueSingleVariable();
                                val.censorCode = "nc";

                                val.dateTime = firstDayInMonth.AddDays(d);
                                val.dateTimeUTC = val.dateTime;
                                val.dateTimeUTCSpecified = true;
                                //val.timeOffset = "00:00";
                                //val.timeOffsetSpecified = false;
                                val.methodCode = methodID.ToString();
                                val.methodID = methodID.ToString();
                                
                                val.offsetValueSpecified = false;
                                val.qualityControlLevelCode = qcID.ToString();
                                val.sourceCode = sourceID.ToString();

                                var originalVal = Convert.ToDecimal(line.Substring(valueCols[d], valueChars));

                                if (originalVal < -9998)
                                {
                                    val.Value = originalVal;
                                }
                                else
                                {
                                    val.Value = Convert.ToDecimal(line.Substring(valueCols[d], valueChars)) / 10.0M;
                                }

                                // qualifiers: each value can have between zero and three qualifiers
                                var qualifiers = "";
                                var mFlag = line.Substring(mflagCols[d], 1).Trim();
                                var qFlag = line.Substring(qflagCols[d], 1).Trim();
                                var sFlag = line.Substring(sflagCols[d], 1).Trim();

                                mFlag = (mFlag == String.Empty) ? "_" : mFlag;
                                qFlag = (qFlag == String.Empty) ? "_" : qFlag;
                                sFlag = (sFlag == String.Empty) ? "_" : sFlag;

                                // to better distinguish lowercase and uppercase SFLAG qualifier
                                if (Char.IsLower(sFlag.ToCharArray()[0]))
                                {
                                    sFlag = sFlag + "1";
                                }

                                qualifiers = "m" + mFlag + "q" + qFlag + "s" + sFlag;

                                if (!usedQualifiers.ContainsKey(qualifiers))
                                {
                                    usedQualifiers.Add(qualifiers, new QualifierType());
                                }

                                val.qualifiers = qualifiers;
                                valuesList.Add(val);
                            }
                        }
                    }
                }
            }
            s.value = valuesList.ToArray();

            // generate qualifier descriptions from combined qualifiers
            var qualList = new List<QualifierType>();
            foreach (var usedQual in usedQualifiers)
            {
                var usedCode = usedQual.Key;

                var usedDesc = allQualifiers.ContainsKey(usedCode) ? allQualifiers[usedCode].qualifierDescription : "Unknown";

                var newQ = new QualifierType();
                newQ.qualifierCode = usedCode;
                newQ.qualifierDescription = usedDesc;
                qualList.Add(newQ);
            }
            s.qualifier = qualList.ToArray();
            return s;
        }

        private static Decimal convertValue(object val, int varId)
        {
            var dVal = Convert.ToDouble(val);
            switch (varId)
            {
                case 1:
                    // precipitation - no data value is now displayed as zero
                    return Convert.ToDecimal(Math.Round(dVal, 1));
                case 4:
                    // water stag
                    return Convert.ToDecimal(Math.Round(dVal, 4));
                case 5:
                    // discharge
                    return Convert.ToDecimal(Math.Round(dVal, 4));
                case 8:
                    // snow
                    return Convert.ToDecimal(Math.Round(dVal, 1));
                case 16:
                case 17:
                case 18:
                    // air temperature
                    return Convert.ToDecimal(Math.Round(dVal, 1));
                default:
                    return Convert.ToDecimal(Math.Round(dVal, 4));
            }
        }

        private static ValueSingleVariable CreateNoDataValue(DateTime time, TsValuesSingleVariableType s, int variableId)
        {
            ValueSingleVariable v = new ValueSingleVariable();
            v.censorCode = "nc";
            v.dateTime = Convert.ToDateTime(time);
            v.dateTimeUTC = v.dateTime.AddHours(-1);
            v.dateTimeUTCSpecified = true;
            //v.methodCode = s.method[0].methodCode;
            //v.methodID = v.methodCode;
            v.methodCode = "0";
            v.offsetValueSpecified = false;
            v.qualityControlLevelCode = "1";
            v.sourceCode = "1";
            //v.sourceID = "1";
            v.timeOffset = "01:00";

            switch (variableId)
            {
                case 1:
                    //for precipitation, set 'no data' to zero
                    v.Value = 0.0M;
                    break;
                case 4:
                    v.Value = -9999.0M;
                    break;
                case 5:
                    v.Value = -9999.0M;
                    break;
                case 8:
                    v.Value = 0.0M;
                    break;
                case 16:
                    v.Value = -9999.0M;
                    break;
                default:
                    v.Value = -9999.0M;
                    break;
            }
            return v;
        }




        /// <summary>
        /// Gets the sites, in XML format
        /// </summary>
        public static SiteInfoResponseTypeSite[] GetSitesFromDb_old()
        {
            List<SiteInfoResponseTypeSite> siteList = new List<SiteInfoResponseTypeSite>();

            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = "SELECT * FROM dbo.Sites";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        SiteInfoResponseTypeSite newSite = new SiteInfoResponseTypeSite();
                        SiteInfoType si = new SiteInfoType();

                        if (dr["Elevation_m"] != DBNull.Value)
                        {
                            si.elevation_m = Math.Round(Convert.ToDouble(dr["Elevation_m"]), 1);
                            si.elevation_mSpecified = true;
                        }
                        else
                        {
                            si.elevation_m = 0;
                            si.elevation_mSpecified = true;
                        }
                        si.geoLocation = new SiteInfoTypeGeoLocation();

                        LatLonPointType latLon = new LatLonPointType();
                        latLon.latitude = Math.Round(Convert.ToDouble(dr["Latitude"]), 4);
                        latLon.longitude = Math.Round(Convert.ToDouble(dr["Longitude"]), 4);
                        latLon.srs = "EPSG:4326";
                        si.geoLocation.geogLocation = latLon;
                        //si.geoLocation.localSiteXY = new SiteInfoTypeGeoLocationLocalSiteXY[1];
                        //si.geoLocation.localSiteXY[0] = new SiteInfoTypeGeoLocationLocalSiteXY();
                        //si.geoLocation.localSiteXY[0].X = latLon.longitude;
                        //si.geoLocation.localSiteXY[0].Y = latLon.latitude;
                        //si.geoLocation.localSiteXY[0].ZSpecified = false;
                        //si.geoLocation.localSiteXY[0].projectionInformation = si.geoLocation.geogLocation.srs;

                        si.metadataTimeSpecified = false;
                        //si.oid = Convert.ToString(dr["st_id"]);
                        //si.note = new NoteType[1];
                        //si.note[0] = new NoteType();
                        //si.note[0].title = "my note";
                        //si.note[0].type = "custom";
                        //si.note[0].Value = "CHMI-D";
                        si.verticalDatum = "MSL";

                        si.siteCode = new SiteInfoTypeSiteCode[1];
                        si.siteCode[0] = new SiteInfoTypeSiteCode();
                        si.siteCode[0].network = serviceCode;
                        si.siteCode[0].siteID = Convert.ToInt32(dr["SiteID"]);
                        si.siteCode[0].siteIDSpecified = true;
                        si.siteCode[0].Value = Convert.ToString(dr["SiteCode"]);

                        si.siteName = Convert.ToString(dr["SiteName"]);

                        // TODO Add Site Property (country, state, comments)

                        newSite.siteInfo = si;
                        siteList.Add(newSite);
                    }
                }
            }
            return siteList.ToArray();
        }

       
        public static seriesCatalogTypeSeries GetSeriesCatalogFromDb(int siteId, string variableCode)
        {
            VariableInfoType v = GetVariableInfoFromDb(variableCode);

            seriesCatalogTypeSeries s = new seriesCatalogTypeSeries();
            string connStr = GetConnectionString();

            //method
            //s.method = GetMethodFromDb(0);

            //qc level
            s.qualityControlLevel = GetQualityControlFromDb(1);

            //source
            s.source = GetSourceFromDb();

            //value count, begin time, end time, check if values exist
            string cnn = GetConnectionString();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT BeginDateTime, EndDateTime, ValueCount, MethodID, MethodDescription FROM dbo.SeriesCatalog 
                                    WHERE SiteID = @SiteID AND VariableCode = @VariableCode";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@SiteID", siteId));
                    cmd.Parameters.Add(new SqlParameter("@VariableCode", variableCode));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();

                    s.variableTimeInterval = new TimeIntervalType();
                    s.variableTimeInterval.beginDateTime = Convert.ToDateTime(dr["BeginDateTime"]);
                    s.variableTimeInterval.beginDateTimeUTC = Convert.ToDateTime(dr["BeginDateTime"]); ;
                    s.variableTimeInterval.beginDateTimeUTCSpecified = false;

                    // end time: if series is active, the original source shows endTime=2100-01-01
                    // in that case we set endTime to today's date
                    DateTime endTime = Convert.ToDateTime(dr["EndDateTime"]);
                    if (endTime > DateTime.Now.Date)
                    {
                        endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    }
                    s.variableTimeInterval.endDateTime = endTime;
                    s.variableTimeInterval.endDateTimeUTC = endTime;
                    s.variableTimeInterval.endDateTimeUTCSpecified = false;


                    s.valueCount = new seriesCatalogTypeSeriesValueCount();
                    s.valueCount.Value = Convert.ToInt32(dr["ValueCount"]);

                    // method info ...
                    MethodType m = new MethodType();
                    m.methodCode = Convert.ToString(dr["MethodID"]);
                    m.methodID = Convert.ToInt32(m.methodCode);
                    m.methodDescription = Convert.ToString(dr["MethodDescription"]);
                    //m.methodLink = Convert.ToString(dr["MethodLink"]);
                    s.method = m;

                }
            }

            //variable
            s.variable = v;

            //data type, sample medium
            s.dataType = s.variable.dataType;
            s.valueType = v.valueType;
            s.sampleMedium = s.variable.sampleMedium;
            s.generalCategory = s.variable.generalCategory;
            return s;
        }

        public static MethodType GetMethodFromDb(int methodID)
        {
            string cnn = GetConnectionString();
            MethodType m = new MethodType();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT * FROM dbo.Methods WHERE MethodID = @MethodID";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@MethodID", methodID));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    m.methodCode = methodID.ToString();
                    m.methodID = methodID;
                    m.methodDescription = Convert.ToString(dr["MethodDescription"]);
                    m.methodLink = Convert.ToString(dr["MethodLink"]);
                }
            }
            return m;
        }

        public static MethodType GetMethodByCode(string methodCode)
        {
            // in our DB we use "methodLink" field to store the custom method code...
            string cnn = GetConnectionString();
            MethodType m = new MethodType();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT * FROM dbo.Methods WHERE MethodLink = @MethodLink";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@MethodLink", methodCode));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    m.methodCode = Convert.ToString(dr["MethodID"]);
                    m.methodID = Convert.ToInt32(dr["MethodID"]);
                    m.methodDescription = Convert.ToString(dr["MethodDescription"]);
                    m.methodLink = Convert.ToString(dr["MethodLink"]);
                }
            }
            return m;
        }

        public static QualityControlLevelType GetQualityControlFromDb(int qcID)
        {
            string cnn = GetConnectionString();
            QualityControlLevelType q = new QualityControlLevelType();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT * FROM dbo.QualityControlLevels WHERE QualityControlLevelID = @qcID";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@qcID", qcID));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    q.qualityControlLevelID = Convert.ToInt32(dr["QualityControlLevelID"]);
                    q.qualityControlLevelIDSpecified = true;
                    q.qualityControlLevelCode = Convert.ToString(dr["QualityControlLevelID"]);
                    q.definition = Convert.ToString(dr["Definition"]);
                    q.explanation = Convert.ToString(dr["Explanation"]);
                }
            }
            return q;
        }

        public static SourceType GetSourceFromDb()
        {
            string cnn = GetConnectionString();
            SourceType s = new SourceType();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT * FROM dbo.Sources";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Read();
                    s.sourceID = Convert.ToInt32(dr["SourceID"]);
                    s.sourceIDSpecified = true;
                    s.sourceCode = Convert.ToString(dr["SourceID"]);
                    s.sourceDescription = Convert.ToString(dr["SourceDescription"]);
                    s.organization = Convert.ToString(dr["Organization"]);
                    s.citation = Convert.ToString(dr["Citation"]);
                    s.sourceLink = new string[] { Convert.ToString(dr["SourceLink"]) };

                    ContactInformationType contactInfo = new ContactInformationType();
                    contactInfo.address = new string[] { Convert.ToString(dr["Address"]) };
                    contactInfo.contactName = Convert.ToString(dr["ContactName"]);
                    contactInfo.email = new string[] { Convert.ToString(dr["Email"]) };
                    contactInfo.phone = new string[] { Convert.ToString(dr["Phone"]) };

                    s.contactInformation = new ContactInformationType[] { contactInfo };

                }
            }
            return s;
        }


        public static Dictionary<string, QualifierType> GetQualifiersFromDb()
        {
            string cnn = GetConnectionString();
            Dictionary<string, QualifierType> qualifierLookup = new Dictionary<string, QualifierType>();
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"SELECT * FROM dbo.Qualifiers";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        var q = new QualifierType();
                        q.qualifierID = Convert.ToInt32(dr["QualifierID"]);
                        q.qualifierCode = Convert.ToString(dr["QualifierCode"]);
                        q.qualifierDescription = Convert.ToString(dr["QualifierDescription"]);
                        qualifierLookup.Add(q.qualifierCode, q);
                    }
                }
            }
            return qualifierLookup;
        }


        public static VariableInfoType GetVariableInfoFromDb(string variableCode)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];

            if (variableCode.IndexOf(serviceCode) == 0)
            {
                variableCode = variableCode.Substring(serviceCode.Length + 1);
            }

            List<VariableInfoType> variablesList = new List<VariableInfoType>();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"select v.*, u.*, 
tu.UnitsName AS UnitsName1, tu.UnitsAbbreviation as UnitsAbrev1, tu.UnitsType as UnitsType1 FROM dbo.variables v 
inner join dbo.units u on v.VariableUnitsID = u.UnitsID
inner join dbo.units tu on v.TimeUnitsID = tu.UnitsID
WHERE v.VariableCode = @variableCode";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@variableCode", variableCode));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        VariableInfoType varInfo = new VariableInfoType();

                        //time support and time unit (same for all variables here)
                        varInfo.timeScale = new VariableInfoTypeTimeScale();
                        varInfo.timeScale.isRegular = true;
                        varInfo.timeScale.timeSpacingSpecified = false;
                        varInfo.timeScale.timeSupport = Convert.ToSingle(dr["TimeSupport"]);
                        varInfo.timeScale.timeSupportSpecified = true;
                        varInfo.timeScale.unit = new UnitsType();
                        varInfo.timeScale.unit.unitAbbreviation = (string)dr["UnitsAbrev1"];
                        varInfo.timeScale.unit.unitCode = Convert.ToString(dr["TimeUnitsID"]);
                        varInfo.timeScale.unit.unitID = (int)dr["TimeUnitsID"];
                        varInfo.timeScale.unit.unitIDSpecified = true;
                        varInfo.timeScale.unit.unitName = (string)dr["UnitsName1"];
                        varInfo.timeScale.unit.unitType = (string)dr["UnitsType1"];
                        varInfo.valueType = (string)dr["ValueType"];

                        varInfo.variableCode = new VariableInfoTypeVariableCode[1];
                        varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
                        varInfo.variableCode[0].@default = true;
                        varInfo.variableCode[0].defaultSpecified = true;
                        varInfo.variableCode[0].Value = (string)dr["VariableCode"];
                        varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
                        varInfo.variableCode[0].variableID = (int)dr["VariableID"];

                        varInfo.dataType = (string)dr["DataType"];
                        varInfo.generalCategory = (string)dr["GeneralCategory"];
                        varInfo.metadataTimeSpecified = false;
                        varInfo.noDataValue = (double)dr["NoDataValue"];
                        varInfo.noDataValueSpecified = true;
                        varInfo.sampleMedium = (string)dr["SampleMedium"];
                        varInfo.speciation = (string)dr["Speciation"];

                        //variable unit
                        varInfo.unit = new UnitsType();
                        varInfo.unit.unitAbbreviation = (string)dr["UnitsAbbreviation"];
                        varInfo.unit.unitCode = Convert.ToString(dr["VariableUnitsID"]);
                        varInfo.unit.unitDescription = (string)dr["UnitsName"];
                        varInfo.unit.unitID = (int)dr["VariableUnitsID"];
                        varInfo.unit.unitIDSpecified = true;
                        varInfo.unit.unitName = (string)dr["UnitsName"];
                        varInfo.unit.unitType = (string)dr["UnitsType"];

                        //variable name
                        varInfo.variableName = (string)dr["VariableName"];
                        variablesList.Add(varInfo);

                    }
                    conn.Close();
                }
            }
            return variablesList[0];
        }


        private static List<string> FindExcludedDurations()
        {
            string exclude_durations_str = ConfigurationManager.AppSettings["exclude_durations"];

            var exclude_durations = new string[] { "" };
            var durationsToExclude = new List<string>();
            if (exclude_durations_str != null)
            {
                exclude_durations = exclude_durations_str.Split(',');
                for (int j = 0; j < exclude_durations.Length; j++)
                {
                    durationsToExclude.Add(exclude_durations[j].Trim());
                }
            }
            return durationsToExclude;
        }


        public static SiteInfoType GetSiteFromDb2(string siteCode)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            SiteInfoType si = new SiteInfoType();

            if (siteCode.StartsWith(serviceCode))
            {
                siteCode = siteCode.Substring(serviceCode.Length + 1);
            }

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sqlSite = "SELECT * FROM dbo.Sites WHERE SiteCode=@siteCode";

                    cmd.CommandText = sqlSite;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@siteCode", siteCode));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    dr.Read();
                    if (dr.HasRows)
                    {
                        if (dr["Elevation_m"] != DBNull.Value)
                        {
                            si.elevation_m = Math.Round(Convert.ToDouble(dr["Elevation_m"]), 1);
                            si.elevation_mSpecified = true;
                        }
                        else
                        {
                            si.elevation_m = 0;
                            si.elevation_mSpecified = true;
                        }
                        si.geoLocation = new SiteInfoTypeGeoLocation();

                        LatLonPointType latLon = new LatLonPointType();
                        latLon.latitude = Math.Round(Convert.ToDouble(dr["Latitude"]), 4);
                        latLon.longitude = Math.Round(Convert.ToDouble(dr["Longitude"]), 4);
                        latLon.srs = "EPSG:4326";
                        si.geoLocation.geogLocation = latLon;
                        //si.geoLocation.localSiteXY = new SiteInfoTypeGeoLocationLocalSiteXY[1];
                        //si.geoLocation.localSiteXY[0] = new SiteInfoTypeGeoLocationLocalSiteXY();
                        //si.geoLocation.localSiteXY[0].X = latLon.longitude;
                        //si.geoLocation.localSiteXY[0].Y = latLon.latitude;
                        //si.geoLocation.localSiteXY[0].ZSpecified = false;
                        //si.geoLocation.localSiteXY[0].projectionInformation = si.geoLocation.geogLocation.srs;

                        si.metadataTimeSpecified = false;
                        //si.oid = Convert.ToString(dr["st_id"]);
                        //si.note = new NoteType[1];
                        //si.note[0] = new NoteType();
                        //si.note[0].title = "my note";
                        //si.note[0].type = "custom";
                        //si.note[0].Value = "CHMI-D";
                        si.verticalDatum = "MSL";

                        si.siteCode = new SiteInfoTypeSiteCode[1];
                        si.siteCode[0] = new SiteInfoTypeSiteCode();
                        si.siteCode[0].network = serviceCode;
                        si.siteCode[0].siteID = Convert.ToInt32(dr["SiteID"]);
                        si.siteCode[0].siteIDSpecified = true;
                        si.siteCode[0].Value = Convert.ToString(dr["SiteCode"]);

                        si.siteName = Convert.ToString(dr["SiteName"]);
                    }
                }
            }
            return si;
        }

        public static SiteInfoResponseTypeSite GetSiteFromDb(string siteId, bool includeSeriesCatalog)
        {
            SiteInfoResponseTypeSite newSite = new SiteInfoResponseTypeSite();

            newSite.siteInfo = GetSiteFromDb2(siteId);

            //to add the catalog
            if (includeSeriesCatalog)
            {
                //List<int> variableIdList = GetVariablesForSite(Convert.ToInt32(newSite.siteInfo.siteCode[0].siteID));
                List<string> variableCodesList = GetVariableCodesForSite(Convert.ToInt32(newSite.siteInfo.siteCode[0].siteID));
                int numVariables = variableCodesList.Count;

                newSite.seriesCatalog = new seriesCatalogType[1];
                newSite.seriesCatalog[0] = new seriesCatalogType();

                List<seriesCatalogTypeSeries> seriesCatalogList = new List<seriesCatalogTypeSeries>();

                for (int i = 0; i < numVariables; i++)
                {
                    int siteID = newSite.siteInfo.siteCode[0].siteID;
                    seriesCatalogTypeSeries cat = GetSeriesCatalogFromDb(siteID, variableCodesList[i]);
                    if (cat != null)
                    {
                        seriesCatalogList.Add(cat);
                    }
                }

                newSite.seriesCatalog[0].series = seriesCatalogList.ToArray();
            }

            return newSite;
        }

        private static List<int> GetVariablesForSite(int siteId)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            List<int> variableIdList = new List<int>();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = "SELECT VariableID FROM dbo.SeriesCatalog WHERE SiteID=@siteID";

                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@siteID", siteId));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        if (dr.HasRows)
                        {
                            int varId = Convert.ToInt32(dr["VariableID"]);
                            if (!variableIdList.Contains(varId))
                                variableIdList.Add(varId);
                        }
                    }
                }
            }
            return variableIdList;
        }

        private static List<string> GetVariableCodesForSite(int siteId)
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            List<string> variableCodeList = new List<string>();

            List<string> excluded = FindExcludedDurations();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = "SELECT VariableCode FROM dbo.SeriesCatalog WHERE SiteID=@siteID";

                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    cmd.Parameters.Add(new SqlParameter("@siteID", siteId));
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        if (dr.HasRows)
                        {
                            string varCode = Convert.ToString(dr["VariableCode"]);
                            variableCodeList.Add(varCode);
                        }
                    }
                }
            }
            return variableCodeList;
        }


        private static QualifierType FlagToQualifier(string flag)
        {
            switch (flag.ToLower())
            {
                case "v":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 1,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Validated Data"
                    };
                case "n":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 2,
                        qualifierIDSpecified = false,
                        qualifierDescription = "No profile for automated validation"
                    };
                case "e":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 3,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Edit, minor adjustment for sensor noise"
                    };
                case "b":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 4,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Regression-based estimate for homogenizing collocated Snow Course and Snow Pillow data sets"
                    };
                case "k":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 5,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Estimate"
                    };
                case "x":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 6,
                        qualifierIDSpecified = false,
                        qualifierDescription = "External estimate"
                    };
                case "s":
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 7,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Suspect data"
                    };
                default:
                    return new QualifierType
                    {
                        qualifierCode = flag,
                        qualifierID = 0,
                        qualifierIDSpecified = false,
                        qualifierDescription = "Unknown"
                    };
            }
        }

    }
}