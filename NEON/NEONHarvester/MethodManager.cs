using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Net;
using System.Globalization;
using OfficeOpenXml;
using System.Linq;

namespace NEONHarvester
{
    /// <summary>
    /// Responsible for updating the Methods table in the ODM
    /// </summary>
    class MethodManager
    {
        private LogWriter _log;
        private NeonApiReader _apiReader;

        public MethodManager(LogWriter log)
        {
            _log = log;
            _apiReader = new NeonApiReader(_log);
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


        public void UpdateMethods()
        {
            LookupFileReader xlsReader = new LookupFileReader(_log);

            Dictionary<string, MethodInfo> methodLookup = xlsReader.ReadMethodsFromExcel();

            foreach (string productCode in methodLookup.Keys)
            {
                var productInfo = _apiReader.ReadProductFromApi(productCode);
                var productDesc = productInfo.productDescription;
                methodLookup[productCode].MethodDescription = productDesc;
            }

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
    }
}
