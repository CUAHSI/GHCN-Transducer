﻿using System;
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

        public MethodManager(LogWriter log, int sleepTime)
        {
            _log = log;
            _apiReader = new NeonApiReader(_log, sleepTime);
        }


        private object SaveOrUpdateMethod(MethodInfo meth, SqlConnection connection)
        {
            object methodIDResult = null;

            using (SqlCommand cmd = new SqlCommand("SELECT MethodID FROM Methods WHERE MethodCode = @code", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@code", meth.MethodCode));
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

            List<MethodInfo> methods = xlsReader.ReadMethodsFromExcel();

            _log.LogWrite(String.Format("Found {0} distinct methods.", methods.Count));
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                // to remove any old ("unused") methods
                DeleteOldMethods(connection);

                foreach (MethodInfo meth in methods)
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
            _log.LogWrite("UpdateMethods OK: " + methods.Count.ToString() + " methods.");
        }


        public void DeleteOldMethods(SqlConnection connection)
        {
            string sqlCount = "SELECT COUNT(*) FROM dbo.Methods";
            int variablesCount = 0;
            using (var cmd = new SqlCommand(sqlCount, connection))
            {
                try
                {
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    variablesCount = Convert.ToInt32(result);
                    Console.WriteLine("number of old methods to delete " + result.ToString());
                }
                catch (Exception ex)
                {
                    var msg = "finding methods count " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }

            string sqlDelete = "DELETE FROM dbo.Methods";

            using (SqlCommand cmd = new SqlCommand(sqlDelete, connection))
            {
                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("deleting old methods ... ");
                }
                catch (Exception ex)
                {
                    var msg = "error deleting old methods " + " " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }

            // reset method ID
            string sqlReset = @"DBCC CHECKIDENT('dbo.Methods', RESEED, 1);";
            using (SqlCommand cmd = new SqlCommand(sqlReset, connection))
            {
                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("reset id of Methods Table");

                }
                catch (Exception ex)
                {
                    var msg = "error deleting old Methods table: " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}
