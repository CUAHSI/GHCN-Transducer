using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace GhcnHarvester
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
                var q = new List<GhcnQualifier>();

                // Qualifiers for MFLAG - the measurement flag
                var qm = new List<GhcnQualifier>();
                qm.Add(new GhcnQualifier("m_", "no measurement information applicable"));
                qm.Add(new GhcnQualifier("mB", "precipitation total formed from two 12 - hour totals"));
                qm.Add(new GhcnQualifier("mD", "precipitation total formed from four six-hour totals"));
                qm.Add(new GhcnQualifier("mH", "represents highest or lowest hourly temperature(TMAX or TMIN) or the average of hourly values (TAVG)"));
                qm.Add(new GhcnQualifier("mK", "converted from knots"));
                qm.Add(new GhcnQualifier("mL", "temperature appears to be lagged with respect to reported hour of observation"));
                qm.Add(new GhcnQualifier("mO", "converted from oktas"));
                qm.Add(new GhcnQualifier("mP", "identified as 'missing presumed zero' in DSI 3200 and 3206"));
                qm.Add(new GhcnQualifier("mT", "trace of precipitation, snowfall, or snow depth"));
                qm.Add(new GhcnQualifier("mW", "converted from 16 - point WBAN code(for wind direction)"));

                // Qualifiers for QFLAG - the the quality flag
                var qq = new List<GhcnQualifier>();
                qq.Add(new GhcnQualifier("q_", "did not fail any quality assurance check"));
                qq.Add(new GhcnQualifier("qD", "failed duplicate check"));
                qq.Add(new GhcnQualifier("qG", "failed gap check"));
                qq.Add(new GhcnQualifier("qI", "failed internal consistency check"));
                qq.Add(new GhcnQualifier("qK", "failed streak/frequent-value check"));
                qq.Add(new GhcnQualifier("qL", "failed check on length of multiday period"));
                qq.Add(new GhcnQualifier("qM", "failed megaconsistency check"));
                qq.Add(new GhcnQualifier("qN", "failed naught check"));
                qq.Add(new GhcnQualifier("qO", "failed climatological outlier check"));
                qq.Add(new GhcnQualifier("qR", "failed lagged range check"));
                qq.Add(new GhcnQualifier("qS", "failed spatial consistency check"));
                qq.Add(new GhcnQualifier("qT", "failed temporal consistency check"));
                qq.Add(new GhcnQualifier("qW", "temperature too warm for snow"));
                qq.Add(new GhcnQualifier("qX", "failed bounds "));
                qq.Add(new GhcnQualifier("qZ", "flagged as a result of an official Datzilla investigation"));

                // Qualifiers for SFLAG - the source flag
                var qs = new List<GhcnQualifier>();
                qs.Add(new GhcnQualifier("s_", "No source (i.e., data value missing)"));
                qs.Add(new GhcnQualifier("s0", "U.S.Cooperative Summary of the Day (NCDC DSI - 3200)"));
                qs.Add(new GhcnQualifier("s6", "CDMP Cooperative Summary of the Day(NCDC DSI - 3206)"));
                qs.Add(new GhcnQualifier("s7", "U.S.Cooperative Summary of the Day --Transmitted via WxCoder3 (NCDC DSI - 3207)"));
                qs.Add(new GhcnQualifier("sA", "U.S.Automated Surface Observing System(ASOS) real - time data(since January 1, 2006)"));
                qs.Add(new GhcnQualifier("sa1", "Australian data from the Australian Bureau of Meteorology"));
                qs.Add(new GhcnQualifier("sB", "U.S.ASOS data for October 2000 - December 2005(NCDC DSI - 3211)"));
                qs.Add(new GhcnQualifier("sb1", "Belarus update"));
                qs.Add(new GhcnQualifier("sC", "Environment Canada"));
                qs.Add(new GhcnQualifier("sE", "European Climate Assessment and Dataset(Klein Tank et al., 2002)"));
                qs.Add(new GhcnQualifier("sF", "U.S.Fort data"));
                qs.Add(new GhcnQualifier("sG", "Official Global Climate Observing System(GCOS) or other government - supplied data"));
                qs.Add(new GhcnQualifier("sH", "High Plains Regional Climate Center real - time data"));
                qs.Add(new GhcnQualifier("sI", "International collection(non U.S.data received through personal contacts)"));
                qs.Add(new GhcnQualifier("sK", "U.S.Cooperative Summary of the Day data digitized from paper observer forms(from 2011 to present)"));
                qs.Add(new GhcnQualifier("sM", "Monthly METAR Extract(additional ASOS data)"));
                qs.Add(new GhcnQualifier("sN", "Community Collaborative Rain, Hail, and Snow(CoCoRaHS)"));
                qs.Add(new GhcnQualifier("sQ", "Data from several African countries that had been 'quarantined', that is, withheld from public release until permission was granted from the respective meteorological services"));
                qs.Add(new GhcnQualifier("sR", "NCEI Reference Network Database(Climate Reference Network and Regional Climate Reference Network)"));
                qs.Add(new GhcnQualifier("sr1", "All-Russian Research Institute of Hydrometeorological Information -World Data Center"));
                qs.Add(new GhcnQualifier("sS", "Global Summary of the Day(NCDC DSI-9618) NOTE: 'S' values are derived from hourly synoptic reports exchanged on the Global Telecommunications System(GTS). Daily values derived in this fashion may differ significantly from 'true' daily data, particularly for precipitation (i.e., use with caution)."));
                qs.Add(new GhcnQualifier("ss1", "China Meteorological Administration/National Meteorological Information Center/ Climatic Data Center(http://cdc.cma.gov.cn)"));
                qs.Add(new GhcnQualifier("sT", "SNOwpack TELemtry (SNOTEL) data obtained from the U.S. Department of Agriculture's Natural Resources Conservation Service"));
                qs.Add(new GhcnQualifier("sU", "Remote Automatic Weather Station(RAWS) data obtained from the Western Regional Climate Center"));
                qs.Add(new GhcnQualifier("su1", "Ukraine update"));
                qs.Add(new GhcnQualifier("sW", "WBAN/ASOS Summary of the Day from NCDC's Integrated Surface Data(ISD)."));
                qs.Add(new GhcnQualifier("sX", "U.S.First-Order Summary of the Day(NCDC DSI-3210)"));
                qs.Add(new GhcnQualifier("sZ", "Datzilla official additions or replacements"));
                qs.Add(new GhcnQualifier("sz1", "Uzbekistan update"));

                // combining the qualifiers into one
                foreach (var q1 in qm)
                {
                    foreach (var q2 in qq)
                    {
                        foreach (var q3 in qs)
                        {
                            q.Add(new GhcnQualifier(q1.QualifierCode + q2.QualifierCode + q3.QualifierCode, q1.QualifierDescription + ", " + q2.QualifierDescription + ", " + q3.QualifierDescription));
                        }
                    }
                }

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (GhcnQualifier qualifier in q)
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


        private object SaveOrUpdateQualifier(GhcnQualifier qualifier, SqlConnection connection)
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
