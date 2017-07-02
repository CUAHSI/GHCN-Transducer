using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using WaterOneFlow.Schema.v1_1;
using WaterOneFlowImpl;

using log4net;
using WaterOneFlowImpl.geom;

namespace WaterOneFlow.odws
{
    //using WaterOneFlow.odm.v1_1;
    using WaterOneFlowImpl.v1_1;
    namespace v1_1
    {
        public class CustomService : IDisposable
        {
            private Cache appCache;

            private HttpContext appContext;

            //private VariablesDataset vds;
            /* This is now done in the global.asax file
            // this got cached, which cause the name to be localhost        
           */
            private string serviceUrl;
            private string serviceName;


            private static readonly ILog log = LogManager.GetLogger(typeof(CustomService));
            private static readonly ILog queryLog = LogManager.GetLogger("QueryLog");
            private static readonly CustomLogging queryLog2 = new CustomLogging();

            public CustomService(HttpContext aContext)
            {

                log.Debug("Starting " + System.Configuration.ConfigurationManager.AppSettings["network"]);
                appContext = aContext;
                // This is now done in the global.asax file
                // this got cached, which cause the name to be localhost
                serviceName = ConfigurationManager.AppSettings["GetValuesName"];
                Boolean odValues = Boolean.Parse(ConfigurationManager.AppSettings["UseODForValues"]);
                if (odValues)
                {
                    string Port = aContext.Request.ServerVariables["SERVER_PORT"];

                    if (Port == null || Port == "80" || Port == "443")

                        Port = "";

                    else

                        Port = ":" + Port;



                    string Protocol = aContext.Request.ServerVariables["SERVER_PORT_SECURE"];

                    if (Protocol == null || Protocol == "0")

                        Protocol = "http://";

                    else

                        Protocol = "https://";





                    // *** Figure out the base Url which points at the application's root

                    serviceUrl = Protocol + aContext.Request.ServerVariables["SERVER_NAME"] +

                                                Port +
                                                aContext.Request.ApplicationPath
                                                + "/" + ConfigurationManager.AppSettings["asmxPage_1_1"] + "?WSDL";

                }
                else
                {
                    serviceUrl = ConfigurationManager.AppSettings["externalGetValuesService"];
                }

            }

            #region Site Information
            public SiteInfoResponseType GetSiteInfo(string locationParameter)
            {
                string siteId = locationParameter.Substring(locationParameter.LastIndexOf(":")+1);
                
                SiteInfoResponseType resp = new SiteInfoResponseType();
                resp.site = new SiteInfoResponseTypeSite[1];
                resp.site[0] = new SiteInfoResponseTypeSite();
                resp.site[0] = WebServiceUtils.GetSiteFromDb(siteId, true);

                resp.queryInfo = CuahsiBuilder.CreateQueryInfoType("GetSiteInfo", new string[] { locationParameter }, null, null, null, null);

                return resp;
            }

            public SiteInfoResponseType GetSiteInfo(string[] locationParameter, Boolean IncludeSeries)
            {
                Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                 if (locationParameter != null)
                {
                    queryLog2.LogStart(CustomLogging.Methods.GetSiteInfo, locationParameter.ToString(),
                                       appContext.Request.UserHostName);
                }
                else
                {
                    queryLog2.LogStart(CustomLogging.Methods.GetSiteInfo, "NULL",
                                       appContext.Request.UserHostName);

                }
                List<locationParam> lpList = new List<locationParam>();
                try
                {
                    foreach (string s in locationParameter)
                    {
                        locationParam l = new locationParam(s);

                        if (l.isGeometry)
                        {
                            String error = "Location by Geometry not accepted: " + locationParameter;
                            log.Debug(error);
                            throw new WaterOneFlowException(error);
                        }
                        else
                        {
                            lpList.Add(l);
                        }
                    }
                }
                catch (WaterOneFlowException we)
                {
                    log.Error(we.Message);
                    throw;
                }
                catch (Exception e)
                {
                    String error =
                        "Sorry. Your submitted site ID for this getSiteInfo request caused an problem that we failed to catch programmatically: " +
                        e.Message;
                    log.Error(error);
                    throw new WaterOneFlowException(error);
                }
                SiteInfoResponseType resp = new SiteInfoResponseType();
                resp.site = new SiteInfoResponseTypeSite[locationParameter.Length];
                for (int i = 0; i < locationParameter.Length; i++)
                {
                    resp.site[i] = WebServiceUtils.GetSiteFromDb(locationParameter[0], true);
                }

                foreach (SiteInfoResponseTypeSite site in resp.site)
                {
                    foreach (seriesCatalogType catalog in site.seriesCatalog)
                    {
                        catalog.menuGroupName = serviceName;
                        catalog.serviceWsdl = serviceUrl;
                    }
                }

                if (locationParameter != null)
                {

                    queryLog2.LogEnd(CustomLogging.Methods.GetSiteInfo,
                                     locationParameter.ToString(),
                                     timer.ElapsedMilliseconds.ToString(),
                                     resp.site.Length.ToString(),
                                     appContext.Request.UserHostName);
                }
                else
                {
                    queryLog2.LogEnd(CustomLogging.Methods.GetSiteInfo,
                                   "NULL",
                                   timer.ElapsedMilliseconds.ToString(),
                                   resp.site.Length.ToString(),
                                   appContext.Request.UserHostName);
                }
 
                return resp;
            }


            public SiteInfoResponseType GetSites(string[] locationIDs)
            {
                Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

                queryLog2.LogStart(CustomLogging.Methods.GetSites, locationIDs.ToString(),
                    appContext.Request.UserHostName);

                SiteInfoResponseType result = new SiteInfoResponseType();
                result.site = WebServiceUtils.GetSitesFromDb();

                //set query info
                result.queryInfo = CuahsiBuilder.CreateQueryInfoType("GetSites");
                NoteType note = CuahsiBuilder.createNote("ALL Sites(empty request)");
                result.queryInfo.note = CuahsiBuilder.addNote(null, note);

                queryLog2.LogEnd(CustomLogging.Methods.GetSites,
                    locationIDs.ToString(),
                    timer.ElapsedMilliseconds.ToString(),
                    result.site.Length.ToString(),
                    appContext.Request.UserHostName);

                return result;
            }

            public SiteInfoResponseType GetSitesInBox(
                float west, float south, float east, float north,
                Boolean IncludeSeries
                )
            {
                Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

                box queryBox = new box(west, south, east, north);

                queryLog2.LogStart(CustomLogging.Methods.GetSitesInBoxObject, queryBox.ToString(),
                     appContext.Request.UserHostName);

                SiteInfoResponseType resp = new SiteInfoResponseType();
                    resp.site = WebServiceUtils.GetSitesByBox(queryBox, IncludeSeries);

                    //set query info
                    resp.queryInfo = CuahsiBuilder.CreateQueryInfoType("GetSitesInBox");
                    NoteType note = CuahsiBuilder.createNote("box");
                    resp.queryInfo.note = CuahsiBuilder.addNote(null, note);

                queryLog2.LogEnd(CustomLogging.Methods.GetSitesInBoxObject,
                      queryBox.ToString(),
                     timer.ElapsedMilliseconds.ToString(),
                     resp.site.Length.ToString(),
                     appContext.Request.UserHostName);
                return resp;
            }
            #endregion


            #region variable

            /// <summary>
            /// GetVariableInfo
            /// </summary>
            /// <param name="VariableParameter">full variable code in format vocabulary:VariableCode</param>
            /// <returns>the VariableInfo object</returns>
            public VariablesResponseType GetVariableInfo(String VariableParameter)
            {
                Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                

                queryLog2.LogStart(CustomLogging.Methods.GetVariables, VariableParameter,
                      appContext.Request.UserHostName);

                VariablesResponseType resp;
                if(string.IsNullOrEmpty(VariableParameter))
                {
                    resp = new VariablesResponseType();
                    resp.variables = WebServiceUtils.GetVariablesFromDb();

                    //setup query info
                    resp.queryInfo = CuahsiBuilder.CreateQueryInfoType
                        ("GetVariableInfo", null, null, new string[] { string.Empty }, null, null);
                    CuahsiBuilder.addNote(resp.queryInfo.note,
                        CuahsiBuilder.createNote("(Request for all variables"));
                }
                else
                {
                    resp = new VariablesResponseType();
                    resp.variables = new VariableInfoType[1];
                    resp.variables[0] = WebServiceUtils.GetVariableInfoFromDb(VariableParameter);
                    //setup query info
                    resp.queryInfo = CuahsiBuilder.CreateQueryInfoType
                        ("GetVariableInfo", null, null, new string[] { VariableParameter }, null, null);
                }

                queryLog2.LogEnd(CustomLogging.Methods.GetVariables,
                    VariableParameter,
                    timer.ElapsedMilliseconds.ToString(),
                    resp.variables.Length.ToString(),
                      appContext.Request.UserHostName);

 
                return resp;
            }

            #endregion

            #region values
            /// <summary>
            /// GetValues custom implementation
            /// </summary>
            /// <param name="SiteNumber">network:SiteCode</param>
            /// <param name="Variable">vocabulary:VariableCode</param>
            /// <param name="StartDate">yyyy-MM-dd</param>
            /// <param name="EndDate">yyyy-MM-dd</param>
            /// <returns></returns>
            public TimeSeriesResponseType GetValues(string SiteNumber,
                                                    string Variable,
                                                    string StartDate,
                                                    string EndDate)
            {
                Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                
                // queryLog.Info("GetValues|" + SiteNumber + "|" + Variable + "|" + StartDate + "|" + EndDate);
                //String network,method,location, variable, start, end, , processing time,count
                queryLog2.LogValuesStart(CustomLogging.Methods.GetValues, // method
                                 SiteNumber, //locaiton
           Variable, //variable
           StartDate, // startdate
           EndDate, //enddate
           appContext.Request.UserHostName);

                //get siteId, variableId
                string siteId = SiteNumber.Substring(SiteNumber.LastIndexOf(":") + 1);
                string variableId = Variable.Substring(Variable.LastIndexOf(":") + 1);

                //get startDateTime, endDateTime
                DateTime startDateTime = new DateTime(1700, 1, 1);
                DateTime endDateTime = DateTime.Now;

                if (StartDate != String.Empty)
                {
                    startDateTime = DateTime.Parse(StartDate);
                }

                if (EndDate != String.Empty)
                {
                    endDateTime = DateTime.Parse(EndDate);
                }
                
                //TimeSeriesResponseType resp = obj.getValues(SiteNumber, Variable, StartDate, EndDate);
                TimeSeriesResponseType resp = new TimeSeriesResponseType();
                resp.timeSeries = new TimeSeriesType[1];
                resp.timeSeries[0] = new TimeSeriesType();
                resp.timeSeries[0].sourceInfo = WebServiceUtils.GetSiteFromDb2(siteId);
                resp.timeSeries[0].variable = WebServiceUtils.GetVariableInfoFromDb(variableId);

                resp.timeSeries[0].values = new TsValuesSingleVariableType[1];
                resp.timeSeries[0].values[0] = WebServiceUtils.GetValuesFromDb(siteId, variableId, startDateTime, endDateTime);
                
                //set the query info
                resp.queryInfo = new QueryInfoType();
                resp.queryInfo.criteria = new QueryInfoTypeCriteria();

                resp.queryInfo.creationTime = DateTime.UtcNow;
                resp.queryInfo.creationTimeSpecified = true;
                resp.queryInfo.criteria.locationParam = SiteNumber;
                resp.queryInfo.criteria.variableParam = Variable;
                resp.queryInfo.criteria.timeParam = CuahsiBuilder.createQueryInfoTimeCriteria(StartDate, EndDate);

                queryLog2.LogValuesEnd(CustomLogging.Methods.GetValues,
                 SiteNumber, //locaiton
           Variable, //variable
           StartDate, // startdate
           EndDate, //enddate
           timer.ElapsedMilliseconds, // processing time
                    // assume one for now 
           resp.timeSeries[0].values[0].value.Length, // count 
                    appContext.Request.UserHostName);

                return resp;

            }

            //TODO implement this function
            public TimeSeriesResponseType GetValuesForASite(string site, string startDate, string endDate)
            {
                {
                    Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

                    //String network,method,location, variable, start, end, , processing time,count
                    queryLog2.LogValuesStart(CustomLogging.Methods.GetValuesForSiteObject, // method
                                     site, //locaiton
                                   "ALL", //variable
                                   startDate, // startdate
                                   endDate, //enddate
                                   appContext.Request.UserHostName);

                    //TimeSeriesResponseType resp = obj.GetValuesForSiteVariable(site, startDate, endDate);
                    TimeSeriesResponseType resp = new TimeSeriesResponseType();

                    //     //String network,method,location, variable, start, end, , processing time,count
                    //     queryLog.InfoFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                    //System.Configuration.ConfigurationManager.AppSettings["network"], // network
                    //"GetValues", // method
                    //SiteNumber, //locaiton
                    //Variable, //variable
                    //StartDate, // startdate
                    //StartDate, //enddate
                    //timer.ElapsedMilliseconds, // processing time
                    //resp.timeSeries.values.value.Length // count 
                    //,
                    //         appContext.Request.UserHostName);
                    queryLog2.LogValuesEnd(CustomLogging.Methods.GetValuesForSiteObject,
                                   site, //locaiton
                                   "ALL", //variable
                                   startDate, // startdate
                                   endDate, //enddate
                                   timer.ElapsedMilliseconds, // processing time
                                            // assume one for now 
                                   -9999, // May need to count all. 
                                   appContext.Request.UserHostName);

                    return resp;

                }
            }

            #endregion

            #region token

            public AuthTokenResponseType GetAuthToken(string userName, string password) 
            {
                string validUser = "admin";
                string validPassword = "1234";

                string validToken = "QwCcA13Ux47ZdwpyX8j";
                
                AuthTokenResponseType resp = new AuthTokenResponseType();

                if (userName == validUser && password == validPassword)
                {
                    resp.Token = validToken;
                    resp.IsValid = true;
                    resp.Expires = DateTime.Now.Date.AddDays(10).ToString("yyyy-mm-dd");
                    resp.Message = "the user is valid";
                }
                else
                {
                    resp.IsValid = false;
                    resp.Token = null;
                    resp.Expires = null;
                    resp.Message = "Invalid userName or password";
                }
                return resp;
            }

            private bool isValidToken(string token) 
            {
                string validToken = "QwCcA13Ux47ZdwpyX8j";
                return (token == validToken);
            }

            #endregion




            #region IDisposable Members

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposeOf)
            {
                // waterLog.Dispose();
            }

            #endregion
        }
    }
}
