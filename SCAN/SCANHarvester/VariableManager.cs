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
    /// Responsible for updating the Variables table in the ODM
    /// </summary>
    class VariableManager
    {
        private LogWriter _log;

        public VariableManager(LogWriter log)
        {
            _log = log;
        }

        public void UpdateVariables()
        {
            string variablesUrl = @"http://raw.githubusercontent.com/CUAHSI/GHCN-Transducer/master/SCAN/SCANHarvester/settings/variables.xlsx";
            var variables = new List<Variable>();
            _log.LogWrite("Read Variables from URL " + variablesUrl);
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
                                string sampleMedium = Convert.ToString(worksheet.Cells[row, 3].Value);
                                string dataType = Convert.ToString(worksheet.Cells[row, 4].Value);
                                string unitsName = Convert.ToString(worksheet.Cells[row, 5].Value);
                                int unitsID = Convert.ToInt32(worksheet.Cells[row, 6].Value);
                                string timeUnitsName = Convert.ToString(worksheet.Cells[row, 7].Value);
                                timeUnitsObj = worksheet.Cells[row, 8].Value;
                                int timeUnitsID = Convert.ToInt32(worksheet.Cells[row, 8].Value);
                                float timeSupport = float.Parse(Convert.ToString(worksheet.Cells[row, 9].Value), CultureInfo.InvariantCulture);

                                Variable v = new Variable
                                {
                                    VariableCode = code,
                                    VariableName = name,
                                    VariableUnitsID = unitsID,
                                    VariableUnitsName = unitsName,
                                    DataType = dataType,
                                    SampleMedium = sampleMedium,
                                    TimeUnitsID = timeUnitsID,
                                    TimeSupport = timeSupport
                                };
                                variables.Add(v);
                            }
                        }
                    }
                }
                _log.LogWrite(String.Format("Found {0} distinct variables.", variables.Count));
                _log.LogWrite(String.Format("Updating {0} variables...", variables.Count));
                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (Variable variable in variables)
                    {
                        try
                        {
                            object variableID = SaveOrUpdateVariable(variable, connection);
                        }
                        catch(Exception ex)
                        {
                            _log.LogWrite("error updating variable: " + variable.VariableCode + " " + ex.Message);
                        }
                        
                    }
                }
                _log.LogWrite("UpdateVariables OK: " + variables.Count.ToString() + " variables.");

            }
            catch (Exception ex)
            {
                _log.LogWrite("UpdateVariables ERROR on row: " + timeUnitsObj.ToString() + " "  + rowNum + " " + ex.Message);
            }

        }

        private object SaveOrUpdateVariable(Variable variable, SqlConnection connection)
        {
            object variableIDResult = null;
            object unitsIDResult = null;

            // getting the units ID by units name
            using (SqlCommand cmd = new SqlCommand("SELECT UnitsID FROM Units WHERE UnitsName = @name", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@name", variable.VariableUnitsName));
                connection.Open();
                unitsIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (unitsIDResult == null)
            {
                throw new ArgumentNullException("Units " + variable.VariableUnitsName + " do not exist in the ODM database!");
            }


            using (SqlCommand cmd = new SqlCommand("SELECT VariableID FROM Variables WHERE VariableCode = @code", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                connection.Open();
                variableIDResult = cmd.ExecuteScalar();
                connection.Close();
            }

            if (variableIDResult != null)
            {
                //update the variable
                variable.VariableID = Convert.ToInt32(variableIDResult);
                using (SqlCommand cmd = new SqlCommand("UPDATE Variables SET VariableName = @name, VariableUnitsID=@units, SampleMedium =@sampleMedium, DataType=@dataType, ValueType=@valueType WHERE VariableCode = @code", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                    cmd.Parameters.Add(new SqlParameter("@name", variable.VariableName));
                    cmd.Parameters.Add(new SqlParameter("@units", unitsIDResult));
                    cmd.Parameters.Add(new SqlParameter("@sampleMedium", variable.SampleMedium));
                    cmd.Parameters.Add(new SqlParameter("@dataType", variable.DataType));
                    cmd.Parameters.Add(new SqlParameter("@valueType", variable.ValueType));
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();

                }
            }
            else
            {
                //save the variable
                string sql = "INSERT INTO Variables(VariableCode, VariableName, Speciation, VariableUnitsID, SampleMedium, ValueType, IsRegular, TimeSupport, TimeUnitsID, DataType, GeneralCategory, NoDataValue) VALUES (@VariableCode, @VariableName, @Speciation, @VariableUnitsID, @SampleMedium, @ValueType, @IsRegular, @TimeSupport, @TimeUnitsID, @DataType, @GeneralCategory, @NoDataValue)";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    cmd.Parameters.Add(new SqlParameter("@VariableCode", variable.VariableCode));
                    cmd.Parameters.Add(new SqlParameter("@VariableName", variable.VariableName));
                    cmd.Parameters.Add(new SqlParameter("@Speciation", variable.Speciation));
                    cmd.Parameters.Add(new SqlParameter("@VariableUnitsID", unitsIDResult));
                    cmd.Parameters.Add(new SqlParameter("@SampleMedium", variable.SampleMedium));
                    cmd.Parameters.Add(new SqlParameter("@ValueType", variable.ValueType));
                    cmd.Parameters.Add(new SqlParameter("@IsRegular", variable.IsRegular));
                    cmd.Parameters.Add(new SqlParameter("@TimeSupport", variable.TimeSupport));
                    cmd.Parameters.Add(new SqlParameter("@TimeUnitsID", variable.TimeUnitsID));
                    cmd.Parameters.Add(new SqlParameter("@DataType", variable.DataType));
                    cmd.Parameters.Add(new SqlParameter("@GeneralCategory", variable.GeneralCategory));
                    cmd.Parameters.Add(new SqlParameter("@NoDataValue", variable.NoDataValue));

                    // to get the inserted variable id
                    SqlParameter param = new SqlParameter("@VariableID", SqlDbType.Int);
                    param.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(param);

                    cmd.ExecuteNonQuery();
                    variableIDResult = cmd.Parameters["@VariableID"].Value;
                    connection.Close();
                }
            }
            return variableIDResult;
        }
    }
}
