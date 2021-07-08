using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Net;

using System.Linq;

namespace CDEC_Harvester
{
    /// <summary>
    /// Responsible for updating the Methods table in the ODM
    /// </summary>
    class MethodManager
    {
        private LogWriter _log;

        public MethodManager(LogWriter log)
        {
            _log = log;
        }

        public void UpdateMethods()
        {
            var methodLookup = new Dictionary<string, MethodInfo>();
            var methodCodes = new List<string>();
           // _log.LogWrite("Read Method Codes from URL " + variablesUrl);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";
            try
            {
                //TO DO Method retrieval 
                    
                _log.LogWrite(String.Format("Found {0} distinct methods.", methodLookup.Count));
                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (MethodInfo meth in methodLookup.Values)
                    {
                        try
                        {
                            object methodID = SaveOrUpdateMethod(meth, connection);
                        }
                        catch (Exception ex)
                        {
                            _log.LogWrite("error updating method: " + meth.MethodDescription + " " + ex.Message);
                        }

                    }
                }
                _log.LogWrite("UpdateMethods OK: " + methodLookup.Count.ToString() + " methods.");

            }
            catch (Exception ex)
            {
                _log.LogWrite("UpdateMethods ERROR: "  + " " + ex.Message);
            }

        }

        private object SaveOrUpdateMethod(MethodInfo meth, SqlConnection connection)
        {
            object methodIDResult = null;

            using (SqlCommand cmd = new SqlCommand("SELECT MethodID FROM Methods WHERE MethodDescription = @desc", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@desc", meth.MethodDescription));
                connection.Open();
                methodIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (methodIDResult != null)
            {
                //update the method
                meth.MethodID = Convert.ToInt32(methodIDResult);
                using (SqlCommand cmd = new SqlCommand("UPDATE Methods SET MethodDescription = @desc, MethodLink = @link, MethodCode = @code WHERE MethodID = @id", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@desc", meth.MethodDescription));
                    cmd.Parameters.Add(new SqlParameter("@link", meth.MethodLink));
                    cmd.Parameters.Add(new SqlParameter("@id", meth.MethodID));
                    cmd.Parameters.Add(new SqlParameter("@code", meth.MethodCode));

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the method
                string sql = "INSERT INTO Methods(MethodDescription, MethodLink, MethodCode) VALUES (@desc, @link, @code)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@desc", meth.MethodDescription));
                    cmd.Parameters.Add(new SqlParameter("@link", meth.MethodLink));
                    cmd.Parameters.Add(new SqlParameter("@code", meth.MethodCode));

                    // to get the inserted method id
                    SqlParameter param = new SqlParameter("@MethodID", SqlDbType.Int);
                    param.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(param);

                    cmd.ExecuteNonQuery();
                    methodIDResult = cmd.Parameters["@MethodID"].Value;
                    connection.Close();
                }
            }
            return methodIDResult;
        }
    }
}
