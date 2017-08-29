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

namespace SCANHarvester
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
            string variablesUrl = @"http://raw.githubusercontent.com/CUAHSI/GHCN-Transducer/master/SCAN/SCANHarvester/settings/variables.xlsx";
            var methodLookup = new Dictionary<string, MethodInfo>();
            var methodCodes = new List<string>();
            _log.LogWrite("Read Method Codes from URL " + variablesUrl);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";
            try
            {
                var webRequest = HttpWebRequest.Create(variablesUrl) as HttpWebRequest;
                var webResponse = webRequest.GetResponse();

                using (var webResponseStream = webResponse.GetResponseStream())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        webResponseStream.CopyTo(memoryStream);
                        using (var package = new ExcelPackage(memoryStream))
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();
                            var start = worksheet.Dimension.Start;
                            var end = worksheet.Dimension.End;
                            for (int row = start.Row; row <= end.Row; row++)
                            { // Row by row..
                                rowNum++;
                                string code = Convert.ToString(worksheet.Cells[row, 1].Value);
                                if (code == "VariableCode")
                                {
                                    continue;
                                }
                                string name = Convert.ToString(worksheet.Cells[row, 2].Value);
                                if (name == "x")
                                {
                                    continue;
                                }
                                // extract the method code from the name
                                string[] codeSplit = code.Split('_');
                                string methodCode = "NOTSPECIFIED";
                                string methodDesc = "No method specified";
                                MethodInfo newMethod;
                                if (codeSplit.Length > 2)
                                {
                                    methodCode = codeSplit[2];

                                    if (methodCode.StartsWith("D"))
                                    {
                                        methodDesc = String.Format("Measured at {0} inches depth below ground surface", methodCode.Substring(1));
                                    }
                                    else
                                    {
                                        methodDesc = String.Format("Measured at {0} feet height above ground surface", methodCode.Substring(1));
                                    }
                                }
                                else
                                {
                                    methodCode = "NOTSPECIFIED";
                                }

                                newMethod = new MethodInfo
                                {
                                    MethodDescription = methodDesc
                                };

                                if (!methodLookup.ContainsKey(methodCode))
                                {
                                    methodLookup.Add(methodCode, newMethod);
                                }
                            }
                        }
                    }
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
                using (SqlCommand cmd = new SqlCommand("UPDATE Methods SET MethodDescription = @desc WHERE MethodID = @id", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@desc", meth.MethodDescription));
                    cmd.Parameters.Add(new SqlParameter("@id", meth.MethodID));
                    
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the method
                string sql = "INSERT INTO Methods(MethodDescription) VALUES (@desc)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@desc", meth.MethodDescription));
                    
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
