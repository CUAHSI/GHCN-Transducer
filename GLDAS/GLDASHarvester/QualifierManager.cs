using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace GldasHarvester
{
    class QualifierManager
    {
        private LogWriter _log;

        public QualifierManager(LogWriter log)
        {
            _log = log;
        }

        public void UpdateQualifiers()
        {
            Console.WriteLine("Updating qualifiers ... ");
            try
            {
                var q = new List<GldasQualifier>();

                // Qualifiers for MFLAG - the measurement flag
                var qm = new List<GldasQualifier>();
                qm.Add(new GldasQualifier("m_", "no measurement information applicable"));
                qm.Add(new GldasQualifier("mB", "precipitation total formed from two 12 - hour totals"));
                qm.Add(new GldasQualifier("mD", "precipitation total formed from four six-hour totals"));
                qm.Add(new GldasQualifier("mH", "represents highest or lowest hourly temperature(TMAX or TMIN) or the average of hourly values (TAVG)"));
                qm.Add(new GldasQualifier("mK", "converted from knots"));
                qm.Add(new GldasQualifier("mL", "temperature appears to be lagged with respect to reported hour of observation"));
                qm.Add(new GldasQualifier("mO", "converted from oktas"));
                qm.Add(new GldasQualifier("mP", "identified as 'missing presumed zero' in DSI 3200 and 3206"));
                qm.Add(new GldasQualifier("mT", "trace of precipitation, snowfall, or snow depth"));
                qm.Add(new GldasQualifier("mW", "converted from 16 - point WBAN code(for wind direction)"));

                // Qualifiers for QFLAG - the the quality flag
                var qq = new List<GldasQualifier>();
                qq.Add(new GldasQualifier("q_", "did not fail any quality assurance check"));
                qq.Add(new GldasQualifier("qD", "failed duplicate check"));
                qq.Add(new GldasQualifier("qG", "failed gap check"));
                qq.Add(new GldasQualifier("qI", "failed internal consistency check"));
                qq.Add(new GldasQualifier("qK", "failed streak/frequent-value check"));
                qq.Add(new GldasQualifier("qL", "failed check on length of multiday period"));
                qq.Add(new GldasQualifier("qM", "failed megaconsistency check"));
                qq.Add(new GldasQualifier("qN", "failed naught check"));
                qq.Add(new GldasQualifier("qO", "failed climatological outlier check"));
                qq.Add(new GldasQualifier("qR", "failed lagged range check"));
                qq.Add(new GldasQualifier("qS", "failed spatial consistency check"));
                qq.Add(new GldasQualifier("qT", "failed temporal consistency check"));
                qq.Add(new GldasQualifier("qW", "temperature too warm for snow"));
                qq.Add(new GldasQualifier("qX", "failed bounds "));
                qq.Add(new GldasQualifier("qZ", "flagged as a result of an official Datzilla investigation"));

                // Qualifiers for SFLAG - the source flag
                var qs = new List<GldasQualifier>();
                qs.Add(new GldasQualifier("s_", "No source (i.e., data value missing)"));
                qs.Add(new GldasQualifier("s0", "U.S.Cooperative Summary of the Day (NCDC DSI - 3200)"));
                qs.Add(new GldasQualifier("s6", "CDMP Cooperative Summary of the Day(NCDC DSI - 3206)"));
                qs.Add(new GldasQualifier("s7", "U.S.Cooperative Summary of the Day --Transmitted via WxCoder3 (NCDC DSI - 3207)"));
                qs.Add(new GldasQualifier("sA", "U.S.Automated Surface Observing System(ASOS) real - time data(since January 1, 2006)"));
                qs.Add(new GldasQualifier("sa1", "Australian data from the Australian Bureau of Meteorology"));
                qs.Add(new GldasQualifier("sB", "U.S.ASOS data for October 2000 - December 2005(NCDC DSI - 3211)"));
                qs.Add(new GldasQualifier("sb1", "Belarus update"));
                qs.Add(new GldasQualifier("sC", "Environment Canada"));
                qs.Add(new GldasQualifier("sE", "European Climate Assessment and Dataset(Klein Tank et al., 2002)"));
                qs.Add(new GldasQualifier("sF", "U.S.Fort data"));
                qs.Add(new GldasQualifier("sG", "Official Global Climate Observing System(GCOS) or other government - supplied data"));
                qs.Add(new GldasQualifier("sH", "High Plains Regional Climate Center real - time data"));
                qs.Add(new GldasQualifier("sI", "International collection(non U.S.data received through personal contacts)"));
                qs.Add(new GldasQualifier("sK", "U.S.Cooperative Summary of the Day data digitized from paper observer forms(from 2011 to present)"));
                qs.Add(new GldasQualifier("sM", "Monthly METAR Extract(additional ASOS data)"));
                qs.Add(new GldasQualifier("sN", "Community Collaborative Rain, Hail, and Snow(CoCoRaHS)"));
                qs.Add(new GldasQualifier("sQ", "Data from several African countries that had been 'quarantined', that is, withheld from public release until permission was granted from the respective meteorological services"));
                qs.Add(new GldasQualifier("sR", "NCEI Reference Network Database(Climate Reference Network and Regional Climate Reference Network)"));
                qs.Add(new GldasQualifier("sr1", "All-Russian Research Institute of Hydrometeorological Information -World Data Center"));
                qs.Add(new GldasQualifier("sS", "Global Summary of the Day(NCDC DSI-9618) NOTE: 'S' values are derived from hourly synoptic reports exchanged on the Global Telecommunications System(GTS). Daily values derived in this fashion may differ significantly from 'true' daily data, particularly for precipitation (i.e., use with caution)."));
                qs.Add(new GldasQualifier("ss1", "China Meteorological Administration/National Meteorological Information Center/ Climatic Data Center(http://cdc.cma.gov.cn)"));
                qs.Add(new GldasQualifier("sT", "SNOwpack TELemtry (SNOTEL) data obtained from the U.S. Department of Agriculture's Natural Resources Conservation Service"));
                qs.Add(new GldasQualifier("sU", "Remote Automatic Weather Station(RAWS) data obtained from the Western Regional Climate Center"));
                qs.Add(new GldasQualifier("su1", "Ukraine update"));
                qs.Add(new GldasQualifier("sW", "WBAN/ASOS Summary of the Day from NCDC's Integrated Surface Data(ISD)."));
                qs.Add(new GldasQualifier("sX", "U.S.First-Order Summary of the Day(NCDC DSI-3210)"));
                qs.Add(new GldasQualifier("sZ", "Datzilla official additions or replacements"));
                qs.Add(new GldasQualifier("sz1", "Uzbekistan update"));

                // combining the qualifiers into one
                foreach (var q1 in qm)
                {
                    foreach (var q2 in qq)
                    {
                        foreach (var q3 in qs)
                        {
                            q.Add(new GldasQualifier(q1.QualifierCode + q2.QualifierCode + q3.QualifierCode, q1.QualifierDescription + ", " + q2.QualifierDescription + ", " + q3.QualifierDescription));
                        }
                    }
                }

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (GldasQualifier qualifier in q)
                    {
                        object qualID = SaveOrUpdateQualifier(qualifier, connection);
                    }
                }
                _log.LogWrite("UpdateQualifiers OK: " + q.Count.ToString() + " qualifiers.");
            }
            catch (Exception ex)
            {
                _log.LogWrite("UpdateQualifiers ERROR: " + ex.Message);
            }
        }


        private object SaveOrUpdateQualifier(GldasQualifier qualifier, SqlConnection connection)
        {
            object qualifierIDResult = null;
            using (SqlCommand cmd = new SqlCommand("SELECT QualifierID FROM dbo.Qualifiers WHERE QualifierCode = @code", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@code", qualifier.QualifierCode));
                connection.Open();
                qualifierIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (qualifierIDResult != null)
            {
                //update the qualifier
                qualifier.QualifierID = Convert.ToInt32(qualifierIDResult);
                using (SqlCommand cmd = new SqlCommand("UPDATE dbo.Qualifiers SET QualifierDescription = @desc WHERE QualifierCode = @code", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@code", qualifier.QualifierCode));
                    cmd.Parameters.Add(new SqlParameter("desc", qualifier.QualifierDescription));
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //add the qualifier
                string sql = "INSERT INTO dbo.Qualifiers(QualifierCode, QualifierDescription) VALUES (@code, @desc)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@code", qualifier.QualifierCode));
                    cmd.Parameters.Add(new SqlParameter("@desc", qualifier.QualifierDescription));

                    // to get the inserted qualifier id
                    SqlParameter param = new SqlParameter("@QualifierID", SqlDbType.Int);
                    param.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(param);

                    cmd.ExecuteNonQuery();
                    qualifierIDResult = cmd.Parameters["@QualifierID"].Value;
                    connection.Close();
                }
            }
            return qualifierIDResult;
        }
    }
}
