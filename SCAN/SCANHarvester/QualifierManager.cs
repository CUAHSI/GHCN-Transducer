using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCANHarvester
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
                var q = new List<Qualifier>();
                q.Add(new Qualifier("V", "Validated Data"));
                q.Add(new Qualifier("N", "No profile for automated validation"));
                q.Add(new Qualifier("E", "Edit, minor adjustment for sensor noise"));
                q.Add(new Qualifier("B", "Regression-based estimate for homogenizing collocated Snow Course and Snow Pillow data sets"));
                q.Add(new Qualifier("K", "Estimate"));
                q.Add(new Qualifier("X", "External estimate"));
                q.Add(new Qualifier("S", "Suspect data"));
                q.Add(new Qualifier("U", "Unknown"));
                q.Add(new Qualifier("R", "No Human Review"));
                q.Add(new Qualifier("P", "Preliminary Human Review"));
                q.Add(new Qualifier("A", "Processing and Final Review Completed"));

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (Qualifier qualifier in q)
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


        private object SaveOrUpdateQualifier(Qualifier qualifier, SqlConnection connection)
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
