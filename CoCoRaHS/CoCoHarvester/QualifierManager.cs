using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoCoHarvester
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
            try
            {
                var q = new List<GhcnQualifier>();

                // Qualifiers for MFLAG - the measurement flag
                q.Add(new GhcnQualifier("m_", "no measurement information applicable"));
                q.Add(new GhcnQualifier("mB", "precipitation total formed from two 12 - hour totals"));
                q.Add(new GhcnQualifier("mD", "precipitation total formed from four six-hour totals"));
                q.Add(new GhcnQualifier("mH", "represents highest or lowest hourly temperature(TMAX or TMIN) or the average of hourly values (TAVG)"));
                q.Add(new GhcnQualifier("mK", "converted from knots"));
                q.Add(new GhcnQualifier("mL", "temperature appears to be lagged with respect to reported hour of observation"));
                q.Add(new GhcnQualifier("mO", "converted from oktas"));
                q.Add(new GhcnQualifier("mP", "identified as 'missing presumed zero' in DSI 3200 and 3206"));
                q.Add(new GhcnQualifier("mT", "trace of precipitation, snowfall, or snow depth"));
                q.Add(new GhcnQualifier("mW", "converted from 16 - point WBAN code(for wind direction)"));

                // Qualifiers for QFLAG - the the quality flag
                q.Add(new GhcnQualifier("q_", "did not fail any quality assurance check"));
                q.Add(new GhcnQualifier("qD", "failed duplicate check"));
                q.Add(new GhcnQualifier("qG", "failed gap check"));
                q.Add(new GhcnQualifier("qI", "failed internal consistency check"));
                q.Add(new GhcnQualifier("qK", "failed streak/frequent-value check"));
                q.Add(new GhcnQualifier("qL", "failed check on length of multiday period"));
                q.Add(new GhcnQualifier("qM", "failed megaconsistency check"));
                q.Add(new GhcnQualifier("qN", "failed naught check"));
                q.Add(new GhcnQualifier("qO", "failed climatological outlier check"));
                q.Add(new GhcnQualifier("qR", "failed lagged range check"));
                q.Add(new GhcnQualifier("qS", "failed spatial consistency check"));
                q.Add(new GhcnQualifier("qT", "failed temporal consistency check"));
                q.Add(new GhcnQualifier("qW", "temperature too warm for snow"));
                q.Add(new GhcnQualifier("qX", "failed bounds "));
                q.Add(new GhcnQualifier("qZ", "flagged as a result of an official Datzilla investigation"));

                // Qualifiers for SFLAG - the source flag
                q.Add(new GhcnQualifier("s__", "No source (i.e., data value missing)"));
                q.Add(new GhcnQualifier("s01", "U.S.Cooperative Summary of the Day (NCDC DSI - 3200)"));
                q.Add(new GhcnQualifier("s61", "CDMP Cooperative Summary of the Day(NCDC DSI - 3206)"));
                q.Add(new GhcnQualifier("s71", "U.S.Cooperative Summary of the Day --Transmitted via WxCoder3 (NCDC DSI - 3207)"));
                q.Add(new GhcnQualifier("sA1", "U.S.Automated Surface Observing System(ASOS) real - time data(since January 1, 2006)"));
                q.Add(new GhcnQualifier("sa2", "Australian data from the Australian Bureau of Meteorology"));
                q.Add(new GhcnQualifier("sB1", "U.S.ASOS data for October 2000 - December 2005(NCDC DSI - 3211)"));
                q.Add(new GhcnQualifier("sb2", "Belarus update"));
                q.Add(new GhcnQualifier("sC1", "Environment Canada"));
                q.Add(new GhcnQualifier("sE1", "European Climate Assessment and Dataset(Klein Tank et al., 2002)"));
                q.Add(new GhcnQualifier("sF1", "U.S.Fort data"));
                q.Add(new GhcnQualifier("sG1", "Official Global Climate Observing System(GCOS) or other government - supplied data"));
                q.Add(new GhcnQualifier("sH1", "High Plains Regional Climate Center real - time data"));
                q.Add(new GhcnQualifier("sI1", "International collection(non U.S.data received through personal contacts)"));
                q.Add(new GhcnQualifier("sK1", "U.S.Cooperative Summary of the Day data digitized from paper observer forms(from 2011 to present)"));
                q.Add(new GhcnQualifier("sM1", "Monthly METAR Extract(additional ASOS data)"));
                q.Add(new GhcnQualifier("sN1", "Community Collaborative Rain, Hail, and Snow(CoCoRaHS)"));
                q.Add(new GhcnQualifier("sQ1", "Data from several African countries that had been 'quarantined', that is, withheld from public release until permission was granted from the respective meteorological services"));
                q.Add(new GhcnQualifier("sR1", "NCEI Reference Network Database(Climate Reference Network and Regional Climate Reference Network)"));
                q.Add(new GhcnQualifier("sr2", "All-Russian Research Institute of Hydrometeorological Information -World Data Center"));
                q.Add(new GhcnQualifier("sS1", "Global Summary of the Day(NCDC DSI-9618) NOTE: 'S' values are derived from hourly synoptic reports exchanged on the Global Telecommunications System(GTS). Daily values derived in this fashion may differ significantly from 'true' daily data, particularly for precipitation (i.e., use with caution)."));
                q.Add(new GhcnQualifier("ss2", "China Meteorological Administration/National Meteorological Information Center/ Climatic Data Center(http://cdc.cma.gov.cn)"));
                q.Add(new GhcnQualifier("sT1", "SNOwpack TELemtry (SNOTEL) data obtained from the U.S. Department of Agriculture's Natural Resources Conservation Service"));
                q.Add(new GhcnQualifier("sU1", "Remote Automatic Weather Station(RAWS) data obtained from the Western Regional Climate Center"));
                q.Add(new GhcnQualifier("su2", "Ukraine update"));
                q.Add(new GhcnQualifier("sW1", "WBAN/ASOS Summary of the Day from NCDC's Integrated Surface Data(ISD)."));
                q.Add(new GhcnQualifier("sX1", "U.S.First-Order Summary of the Day(NCDC DSI-3210)"));
                q.Add(new GhcnQualifier("sZ1", "Datzilla official additions or replacements"));
                q.Add(new GhcnQualifier("sz2", "Uzbekistan update"));

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
