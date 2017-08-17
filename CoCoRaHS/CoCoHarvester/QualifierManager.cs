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
