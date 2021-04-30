using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace GldasHarvester
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
            try
            {

                List<GldasVariable> variables = new List<GldasVariable>();
                variables.Add(new GldasVariable
                {
                    VariableCode = "GLDAS_NOAH025_3H_v2.1:Tair_f_inst",
                    VariableName = "Temperature",
                    VariableUnitsID = 47,
                    SampleMedium = "Air",
                    DataType = "Continuous"
                });

                variables.Add(new GldasVariable
                {
                    VariableCode = "GLDAS_NOAH025_3H_v2.1:Evap_tavg",
                    VariableName = "Evaporation",
                    VariableUnitsID = 47,
                    SampleMedium = "Air",
                    DataType = "Continuous"
                });

                

                string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    foreach (GldasVariable variable in variables)
                    {
                        object variableID = SaveOrUpdateVariable(variable, connection);
                    }
                }
                _log.LogWrite("UpdateVariables OK: " + variables.Count.ToString() + " variables.");
            }
            catch(Exception ex)
            {
                _log.LogWrite("UpdateVariables ERROR: " + ex.Message);
            }
        }

        private object SaveOrUpdateVariable(GldasVariable variable, SqlConnection connection)
        {
            object variableIDResult = null;
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
                    cmd.Parameters.Add(new SqlParameter("@units", variable.VariableUnitsID));
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
                    cmd.Parameters.Add(new SqlParameter("@VariableUnitsID", variable.VariableUnitsID));
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
