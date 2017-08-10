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

namespace WaterOneFlow.odws
{
    /// <summary>
    /// The web service utilities
    /// </summary>
    public class WebServiceUtils
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
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
        /// Gets the sites, in XML format [test for SNOW]
        /// </summary>
        public static SiteInfoResponseTypeSite[] GetSitesFromDb()
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

        /// <summary>
        /// Gets the sites, in XML format [test for SNOW]
        /// </summary>
        public static SiteInfoResponseTypeSite[] GetSitesByBox(box queryBox, bool includeSeries)
        {
            List<SiteInfoResponseTypeSite> siteList = new List<SiteInfoResponseTypeSite>();

            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sqlSites = "SELECT * FROM dbo.Sites WHERE Latitude >= @lat1 AND Latitude <= @lat2 AND Longitude >= @lon1 AND Longitude <= @lon2";
                    
                    //sqlSites = "SELECT plaveninycz.Stations.st_id, st_name, altitude, location_id, lat, lon FROM plaveninycz.Stations INNER JOIN StationsVariables stv ON Stations.st_id = stv.st_id " +                  
                    //"WHERE var_id in (1, 4, 5, 16) AND lat IS NOT NULL";
                    
                    cmd.CommandText = sqlSites;
                    cmd.Connection = conn;

                    cmd.Parameters.Add(new SqlParameter("@lat1", queryBox.South));
                    cmd.Parameters.Add(new SqlParameter("@lat2", queryBox.North));
                    cmd.Parameters.Add(new SqlParameter("@lon1", queryBox.West));
                    cmd.Parameters.Add(new SqlParameter("@lon2", queryBox.East));

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

        public static string VariableIDToCode(int variableID) 
        {
            string prefix = ConfigurationManager.AppSettings["vocabulary"];
            return prefix + ":" + VariableIDToShortCode(variableID);
        }

        public static string VariableIDToShortCode(int variableID) {
            switch (variableID)
            {
                case 1:
                    return "SRAZKY";
                case 4:
                    return "VODSTAV";
                case 5:
                    return "PRUTOK";
                case 8:
                    return "SNIH";
                case 16:
                    return "TEPLOTA";
                case 17:
                    return "TMIN";
                case 18:
                    return "TMAX";
                default:
                    return "UNKNOWN";
            }
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

        private static string GetTableName(int variableId)
        {
            switch(variableId)
            {
                case 1:
                    return "rain_hourly";
                    
                case 4:
                    return "stage";
                    
                case 5:
                    return "discharge";
                    
                case 8:
                    return "snow";
                    
                case 16:
                case 17:
                case 18:
                    return "temperature";
                    
                default:
                    return "rain_hourly";
            }
        }

        public static seriesCatalogTypeSeries GetSeriesCatalogFromDb(int siteId, string variableCode)
        {
            VariableInfoType v = GetVariableInfoFromDb(variableCode);

            seriesCatalogTypeSeries s = new seriesCatalogTypeSeries();
            string connStr = GetConnectionString();

            //method
            s.method = GetMethodFromDb(0);

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
                    string sql = @"SELECT BeginDateTime, EndDateTime FROM dbo.SeriesCatalog 
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

                    s.variableTimeInterval.endDateTime = Convert.ToDateTime(dr["EndDateTime"]);
                    s.variableTimeInterval.endDateTimeUTC = Convert.ToDateTime(dr["EndDateTime"]);
                    s.variableTimeInterval.endDateTimeUTCSpecified = false;

                    double totalDays = (s.variableTimeInterval.endDateTime.Subtract(s.variableTimeInterval.beginDateTime)).TotalDays;
                    s.valueCount = new seriesCatalogTypeSeriesValueCount();
                    s.valueCount.Value = (int)(Math.Round(totalDays)) + 1;

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
                        varInfo.timeScale.timeSupport = 1.0f;
                        varInfo.timeScale.timeSupportSpecified = true;
                        varInfo.timeScale.unit = new UnitsType();
                        varInfo.timeScale.unit.unitAbbreviation = (string)dr["UnitsAbrev1"];
                        varInfo.timeScale.unit.unitCode = Convert.ToString(dr["TimeUnitsID"]);
                        varInfo.timeScale.unit.unitID = (int)dr["TimeUnitsID"]; ;
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


        public static VariableInfoType[] GetVariablesFromDb()
        {
            string cnn = GetConnectionString();
            string serviceCode = ConfigurationManager.AppSettings["network"];
            List<VariableInfoType> variablesList = new List<VariableInfoType>();

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    string sql = @"select v.*, u.*, 
tu.UnitsName AS UnitsName1, tu.UnitsAbbreviation as UnitsAbrev1, tu.UnitsType as UnitsType1 FROM dbo.variables v 
inner join dbo.units u on v.VariableUnitsID = u.UnitsID
inner join dbo.units tu on v.TimeUnitsID = tu.UnitsID";
                    cmd.CommandText = sql;
                    cmd.Connection = conn;
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        VariableInfoType varInfo = new VariableInfoType();

                        //time support and time unit (same for all variables here)
                        varInfo.timeScale = new VariableInfoTypeTimeScale();
                        varInfo.timeScale.isRegular = true;
                        varInfo.timeScale.timeSpacingSpecified = false;
                        varInfo.timeScale.timeSupport = 1.0f;
                        varInfo.timeScale.timeSupportSpecified = true;
                        varInfo.timeScale.unit = new UnitsType();
                        varInfo.timeScale.unit.unitAbbreviation = (string)dr["UnitsAbrev1"];
                        varInfo.timeScale.unit.unitCode = Convert.ToString(dr["TimeUnitsID"]);
                        varInfo.timeScale.unit.unitID = (int)dr["TimeUnitsID"]; ;
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
            return variablesList.ToArray();
        }

        internal static SourceType GetSourceForSite(int siteId) 
        {
            SourceType s = new SourceType();
            s.citation = "CHMI";
            s.organization = "CHMI";
            s.sourceCode = "1";
            s.sourceDescription = " measured by CHMI professional stations";
            s.sourceID = 1;
            s.sourceIDSpecified = true;
            
            string sql = "SELECT op.id, op.name, op.url FROM plaveninycz.operator op " +
                String.Format("INNER JOIN plaveninycz.stations s ON op.id = s.operator_id WHERE s.st_id = {0}", siteId);
            string connStr = GetConnectionString();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        dr.Read();
                        s.citation = Convert.ToString(dr["name"]);
                        s.organization = Convert.ToString(dr["name"]);
                        s.sourceCode = Convert.ToString(dr["id"]);
                        s.sourceLink = new string[1];
                        s.sourceLink[0] = Convert.ToString(dr["url"]);
                        s.sourceID = Convert.ToInt32(dr["id"]);
                    }
                    s.sourceLink = new string[1];
                    s.sourceLink[0] = "http://hydrodata.info/";
                }
            }            
            return s;
        }

        internal static void SetVariableProperties(int var_id, VariableInfoType varInfo)
        {
            //time support and time unit (same for all variables here)
            varInfo.timeScale = new VariableInfoTypeTimeScale();
            varInfo.timeScale.isRegular = true;
            varInfo.timeScale.timeSpacingSpecified = false;
            varInfo.timeScale.timeSupport = 1.0f;
            varInfo.timeScale.timeSupportSpecified = true;
            varInfo.timeScale.unit = new UnitsType();
            varInfo.timeScale.unit.unitAbbreviation = "hr";
            varInfo.timeScale.unit.unitCode = "103";
            varInfo.timeScale.unit.unitID = 103;
            varInfo.timeScale.unit.unitName = "hour";
            varInfo.timeScale.unit.unitType = "Time";

            //variable code (same computation for all variables here)
            varInfo.valueType = "Field Observation";

            varInfo.variableCode = new VariableInfoTypeVariableCode[1];
            varInfo.variableCode[0] = new VariableInfoTypeVariableCode();
            varInfo.variableCode[0].@default = true;
            varInfo.variableCode[0].defaultSpecified = true;
            varInfo.variableCode[0].Value = VariableIDToShortCode(var_id);
            varInfo.variableCode[0].vocabulary = ConfigurationManager.AppSettings["vocabulary"];
            varInfo.variableCode[0].variableID = var_id;
            
            switch(var_id)
            {
                case 1:
                    //precipitation
                    varInfo.dataType = "Incremental";
                    varInfo.generalCategory = "Climate";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Precipitation";
                    varInfo.speciation = "Not Applicable";

                    //variable unit
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "mm";
                    varInfo.unit.unitCode = "54";
                    varInfo.unit.unitDescription = "millimeter";
                    varInfo.unit.unitID = 1;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "millimeter";
                    varInfo.unit.unitType = "Length";
                    varInfo.variableName = "Precipitation";
                    break;
                case 4:
                    //water level
                    varInfo.dataType = "Average";
                    varInfo.generalCategory = "Hydrology";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Surface water";
                    varInfo.speciation = "Not Applicable";

                    //variable unit - centimeter
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "cm";
                    varInfo.unit.unitCode = "47";
                    varInfo.unit.unitDescription = "centimeter";
                    varInfo.unit.unitID = 47;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "centimeter";
                    varInfo.unit.unitType = "Length";

                    //variable value type and name
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Gage height";
                    break;
                case 5:
                    //discharge
                    varInfo.dataType = "Average";
                    varInfo.generalCategory = "Hydrology";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Surface water";
                    varInfo.speciation = "Not Applicable";

                    //variable unit - cubic meter per second
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "m^3/s";
                    varInfo.unit.unitCode = "36";
                    varInfo.unit.unitDescription = "cubic meters per second";
                    varInfo.unit.unitID = 36;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "cubic meters per second";
                    varInfo.unit.unitType = "Flow";
                    
                    //variable value type and name
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Discharge";
                    break;
                case 8:
                    //snow
                    varInfo.dataType = "Average";
                    varInfo.generalCategory = "Climate";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Snow";
                    varInfo.speciation = "Not Applicable";

                    //variable unit
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "cm";
                    varInfo.unit.unitCode = "47";
                    varInfo.unit.unitDescription = "centimeter";
                    varInfo.unit.unitID = 47;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "centimeter";
                    varInfo.unit.unitType = "Length";

                    //variable value type and name
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Snow depth";
                    break;
                case 16:
                    //temperature
                    varInfo.dataType = "Average";
                    varInfo.generalCategory = "Climate";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Air";
                    varInfo.speciation = "Not Applicable";

                    //variable unit
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "degC";
                    varInfo.unit.unitCode = "96";
                    varInfo.unit.unitDescription = "degree celsius";
                    varInfo.unit.unitID = 96;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "degree celsius";
                    varInfo.unit.unitType = "Temperature";

                    //variable code
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Temperature";
                    break;
                case 17:
                    //temperature min
                    varInfo.dataType = "Minimum";
                    varInfo.generalCategory = "Climate";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Air";
                    varInfo.speciation = "Not Applicable";

                    //variable unit
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "degC";
                    varInfo.unit.unitCode = "96";
                    varInfo.unit.unitDescription = "degree celsius";
                    varInfo.unit.unitID = 96;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "degree celsius";
                    varInfo.unit.unitType = "Temperature";

                    //variable code
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Temperature";
                    break;
                case 18:
                    //temperature
                    varInfo.dataType = "Maximum";
                    varInfo.generalCategory = "Climate";
                    varInfo.metadataTimeSpecified = false;
                    varInfo.noDataValue = -9999.0;
                    varInfo.noDataValueSpecified = true;
                    varInfo.sampleMedium = "Air";
                    varInfo.speciation = "Not Applicable";

                    //variable unit
                    varInfo.unit = new UnitsType();
                    varInfo.unit.unitAbbreviation = "degC";
                    varInfo.unit.unitCode = "96";
                    varInfo.unit.unitDescription = "degree celsius";
                    varInfo.unit.unitID = 96;
                    varInfo.unit.unitIDSpecified = true;
                    varInfo.unit.unitName = "degree celsius";
                    varInfo.unit.unitType = "Temperature";

                    //variable code
                    varInfo.valueType = "Field Observation";
                    varInfo.variableName = "Temperature";
                    break;
            }
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
                            if (!variableCodeList.Contains(varCode))
                                variableCodeList.Add(varCode);
                        }
                    }
                }
            }
            return variableCodeList;
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

            //values: get from GHCN .dly file
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
                                //val.timeOffset = "00:00";
                                //val.timeOffsetSpecified = false;
                                val.methodCode = methodID.ToString();
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
                                if (sFlag == String.Empty)
                                {
                                    sFlag = "__";
                                }
                                else
                                {
                                    if (Char.IsLower(sFlag.ToCharArray()[0]))
                                    {
                                        sFlag = sFlag + "2";
                                    }
                                    else
                                    {
                                        sFlag = sFlag + "1";
                                    }
                                }

                                qualifiers = mFlag + qFlag + sFlag;

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
                var usedMFlag = "m" + usedCode.Substring(0, 1);
                var usedQFlag = "q" + usedCode.Substring(1, 1);
                var usedSFlag = "s" + usedCode.Substring(2, 2);

                var mDesc = allQualifiers.ContainsKey(usedMFlag) ? allQualifiers[usedMFlag].qualifierDescription + ", ": "";
                var qDesc = allQualifiers.ContainsKey(usedQFlag) ? allQualifiers[usedQFlag].qualifierDescription + ", ": "";
                var sDesc = allQualifiers.ContainsKey(usedSFlag) ? allQualifiers[usedSFlag].qualifierDescription : "unknown source";

                var newQ = new QualifierType();
                newQ.qualifierCode = usedCode;
                newQ.qualifierDescription = mDesc + qDesc + sDesc;
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
    }
}
