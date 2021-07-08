using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Net;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CsvHelper;
using System.Text;

namespace CDEC_Harvester
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
            //set filename retrieved from https://cdec.water.ca.gov/reportapp/javareports?name=SensList and extended with mappings!
            string fileName = "CDEC_SensorDefinition.csv";
            //Get path to file
            string path = AppDomain.CurrentDomain.BaseDirectory + "/Files/" + fileName;
            //init station list
            var sensors = new List<SensorModel>();

            var variables = new List<Variable>();

            _log.LogWrite("Read Variables from File " + fileName);
            //int rowNum = 0;
            //object timeUnitsObj = "timeUnitsObj";

            try
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<SensorModelClassMap>();
                    sensors = csv.GetRecords<SensorModel>().ToList();
                }

            }
            catch (Exception ex)
            {
                _log.LogWrite("Error: read Stations csv: " + ex.Message);
            }


            

            //_log.LogWrite(String.Format("Found {0} distinct variables.", variables.Count));
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                // to remove any old ("unused") variables
                DeleteOldVariables(connection);
                var variable = new Variable();
                
                //get Units from ODM
                var odmUnits = GetODMUnits();

                //Get VariableCV from ODM
                var odmVariableCV = GetODMVariableCV();

                int unitId;
                string unitName;

                foreach (var sensor in sensors.FindAll(s =>s.ODM_Variable_term != string.Empty)) // skip all tthat have couldnot be matched to a variable TODO: extend units CV 
                {
                    try
                    {
                        //Look up variable name synonymns
                        //var VariableName = (getVariablenameSynonym(sensor.DESCRIPTION.ToLower()) != null) ? getVariablenameSynonym(sensor.DESCRIPTION.ToLower()) : sensor.DESCRIPTION;

                        //look up and map Variableunits
                        //string unitsAbbr = (getUnitSynonym(sensor.UNITS.ToLower()) != null) ? getUnitSynonym(sensor.UNITS.ToLower()) : sensor.UNITS.ToLower();

                        try
                        {
                            unitId = (from u in odmUnits where u.UnitsAbbreviation == sensor.ODM_Unit_abbre select u.UnitsID).First();
                            unitName = (from u in odmUnits where u.UnitsID == unitId select u.UnitsName).First();
                        }
                        catch (Exception ex)
                        {
                            _log.LogWrite(String.Format("ERROR: No match was found: " + sensor.ODM_Unit_abbre + ", " + sensor.DESCRIPTION));
                            continue;
                        }

                        //update sample medium from CD
                        var sampleMedium = "Unknown";

                        if (sensor.DESCRIPTION.ToLower().Contains(" air")) { sampleMedium = "Air"; };
                        if (sensor.DESCRIPTION.ToLower().StartsWith("water")) { sampleMedium = "Surface water"; };
                        if (sensor.DESCRIPTION.ToLower().StartsWith("snow")) { sampleMedium = "Snow"; };
                        if (sensor.DESCRIPTION.ToLower().StartsWith("soil")) { sampleMedium = "Soil"; };
                        if (sensor.DESCRIPTION.ToLower().Contains("grnd wtr")) { sampleMedium = "Groundwater"; };

                        //udate DataType from CV
                        var dataType = "Unknown";
                        if (sensor.DESCRIPTION.ToLower().Contains("median")) { dataType = "Median"; };
                        if (sensor.DESCRIPTION.ToLower().StartsWith("min") || (sensor.DESCRIPTION.ToLower().Contains("minimum"))) { dataType = "Minimum"; };
                        if (sensor.DESCRIPTION.ToLower().StartsWith("max") || (sensor.DESCRIPTION.ToLower().Contains("maximum"))) { dataType = "Maximum"; };
                        if (sensor.DESCRIPTION.ToLower().Contains("accumulated")) { dataType = "Cumulative"; };
                        if (sensor.DESCRIPTION.ToLower().Contains("average")) { dataType = "Average"; };
                        if (sensor.DESCRIPTION.ToLower().Contains("incremental")) { dataType = "Incremental"; };

                        variable.VariableID = sensor.SENSORNUM;
                        variable.VariableCode = sensor.SENSOR.Replace(" ", "_"); ;
                        variable.VariableName = sensor.ODM_Variable_term;
                        variable.VariableUnitsID = unitId;
                        //variable.VariableUnitsName = unitName;
                        variable.DataType = dataType;
                        variable.SampleMedium = sampleMedium;
                        variable.TimeUnitsID = 104; //arbitraily set to 104:day 
                        //variable.TimeSupport = timeSupport


                        object variableID = SaveOrUpdateVariable(variable, connection);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWrite("error updating variable: " + variable.VariableCode + " " + ex.Message);
                    }

                }
            }
            _log.LogWrite("UpdateVariables OK: " + variables.Count.ToString() + " variables.");

        }

        public void DeleteOldVariables(SqlConnection connection)
        {
            string sqlCount = "SELECT COUNT(*) FROM dbo.Variables";
            int variablesCount = 0;
            using (var cmd = new SqlCommand(sqlCount, connection))
            {
                try
                {
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    variablesCount = Convert.ToInt32(result);
                    Console.WriteLine("number of old variables to delete " + result.ToString());
                }
                catch (Exception ex)
                {
                    var msg = "finding variables count " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }

            string sqlDelete = "DELETE FROM dbo.Variables";

            using (SqlCommand cmd = new SqlCommand(sqlDelete, connection))
            {
                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("deleting old variables ... ");
                }
                catch (Exception ex)
                {
                    var msg = "error deleting old variables " + " " + ex.Message;
                    Console.WriteLine(msg);
                    _log.LogWrite(msg);
                    return;
                }
                finally
                {
                    connection.Close();
                }
            }

            // reset variable ID
            string sqlReset = @"DBCC CHECKIDENT('dbo.Variables', RESEED, 1);";
            using (SqlCommand cmd = new SqlCommand(sqlReset, connection))
            {
                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("reset id of Variables Table");

                }
                catch (Exception ex)
                {
                    var msg = "error deleting old Variables table: " + ex.Message;
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

        public void updateVariable (Variable variable, SqlConnection connection)
        {
            //fornow only update timesupport 
            //using (SqlCommand cmd = new SqlCommand("UPDATE Variables SET VariableName = @name, VariableUnitsID=@units, TimeSupport=@timeSupport, SampleMedium=@sampleMedium, DataType=@dataType, ValueType=@valueType WHERE variableId = " + variable.VariableID, connection))
            using (SqlCommand cmd = new SqlCommand("UPDATE Variables SET TimeUnitsID=@timeUnitsID WHERE variableId = " + variable.VariableID, connection))

            {
                //cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                //cmd.Parameters.Add(new SqlParameter("@name", variable.VariableName));
                //cmd.Parameters.Add(new SqlParameter("@units", variable.TimeUnitsID));
                cmd.Parameters.Add(new SqlParameter("@timeUnitsID", variable.TimeUnitsID)); 
                //cmd.Parameters.Add(new SqlParameter("@sampleMedium", variable.SampleMedium));
                //cmd.Parameters.Add(new SqlParameter("@dataType", variable.DataType));
                //cmd.Parameters.Add(new SqlParameter("@valueType", variable.ValueType));
                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();

            }
           
        }

        public object SaveOrUpdateVariable(Variable variable, SqlConnection connection)
        {
            object variableIDResult = null;
            object unitsIDResult = null;

            // getting the units ID by units name
            //using (SqlCommand cmd = new SqlCommand("SELECT UnitsID FROM Units WHERE UnitsName = @name", connection))
            //{
            //    cmd.Parameters.Add(new SqlParameter("@name", variable.VariableUnitsName));
            //    connection.Open();
            //    unitsIDResult = cmd.ExecuteScalar();
            //    connection.Close();
            //}

            //if (unitsIDResult == null)
            //{
            //    throw new ArgumentNullException("Units " + variable.VariableUnitsName + " do not exist in the ODM database!");
            //}
            try
            {

                //using (SqlCommand cmd = new SqlCommand("SELECT VariableID FROM Variables WHERE VariableCode = @code", connection))
                //{
                //    cmd.Parameters.Add(new SqlParameter("@code", variable.VariableCode));
                //    connection.Open();
                //    variableIDResult = cmd.ExecuteScalar();
                //    connection.Close();
                //}

                //if (variableIDResult != null)
                //{
                //update the variable
                //variable.VariableID = Convert.ToInt32(variableIDResult);
                
                //save the variable
                    

                    using (var cmd = connection.CreateCommand())
                    {
                        connection.Open();
                    cmd.CommandText ="SET IDENTITY_INSERT variables ON";
                        cmd.ExecuteNonQuery();

                        string sql = "INSERT INTO Variables(VariableId, VariableCode, VariableName, Speciation, VariableUnitsID, SampleMedium, ValueType, IsRegular, TimeSupport, TimeUnitsID, DataType, GeneralCategory, NoDataValue) VALUES (@variableId, @VariableCode, @VariableName, @Speciation, @VariableUnitsID, @SampleMedium, @ValueType, @IsRegular, @TimeSupport, @TimeUnitsID, @DataType, @GeneralCategory, @NoDataValue)";
                        cmd.CommandText = sql;

                        
                        cmd.Parameters.Add(new SqlParameter("@VariableId", variable.VariableID));
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
                        //SqlParameter param = new SqlParameter("@VariableID", SqlDbType.Int);
                        //param.Direction = ParameterDirection.Output;
                        //cmd.Parameters.Add(param);
                        
                        cmd.ExecuteNonQuery();
                        variableIDResult = cmd.Parameters["@VariableID"].Value;

                        cmd.CommandText = "SET IDENTITY_INSERT variables OFF";
                        cmd.ExecuteNonQuery();

                    connection.Close();
                    }
                //
            }
            catch (Exception ex)
            {                
                connection.Close();
                throw ex;
            }
            return variableIDResult;
        }

        public List<Units> GetODMUnits()
        {
            var odmUnitList = new List<Units>();
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                SqlCommand sqlCommand = new SqlCommand("SELECT * FROM units", cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    var u = new Units();
                    u.UnitsID = (int)reader["UnitsID"];
                    u.UnitsName = (string)reader["UnitsName"];
                    u.UnitsType = (string)reader["UnitsType"];
                    u.UnitsAbbreviation = ((string)reader["UnitsAbbreviation"]);
                    odmUnitList.Add(u);
                }
                cn.Close();
            }

            return odmUnitList;
        }

        public Dictionary<String, String> GetODMVariableCV()
        {
            var ODMVariableCV = new Dictionary<String, String>();//Name, description
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                SqlCommand sqlCommand = new SqlCommand("SELECT * FROM VariableNameCV", cn);
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    ODMVariableCV.Add((string)reader["Term"], (string)reader["Definition"]);
                }
                cn.Close();
            }

            return ODMVariableCV;
        }

        public string getUnitSynonym(string term)
        {
            string syn = String.Empty;
            //adding synanyms for cv entries missing
            var unitAbbreviationSynonyms = new Dictionary<String, String>();//CDEC code, ODM term
            unitAbbreviationSynonyms.Add("feet", "ft");
            unitAbbreviationSynonyms.Add("inches", "in");
            unitAbbreviationSynonyms.Add("deg f", "deg f");
            unitAbbreviationSynonyms.Add("mS/cm", "mS/cm");
            unitAbbreviationSynonyms.Add("af", "ac ft");
            //unitAbbreviationSynonyms.Add("count", "#");
            //unitAbbreviationSynonyms.Add("count/m3", "#/m^3");
            //unitAbbreviationSynonyms.Add("inches", "in");
            //unitAbbreviationSynonyms.Add("acres", "ac");
            //unitAbbreviationSynonyms.Add("kwH", "kW hr");
            //unitAbbreviationSynonyms.Add("kw", "kW");
            //unitAbbreviationSynonyms.Add("mwh", "kW");


            //syn = unitAbbreviationSynonyms.Where (v => v.Key == term).FirstOrDefault().ToString();
            unitAbbreviationSynonyms.TryGetValue(term, out syn);
            return syn;

        }

        public string getVariablenameSynonym(string term)
        {
            string syn = String.Empty;
            //adding synanyms for cv entries missing
            var VariablenameSynonym = new Dictionary<String, String>();//usbr code, ODM term
            VariablenameSynonym.Add("river stage", "Water Level");
            VariablenameSynonym.Add("precipitation, accumulated", "Precipitation");
            VariablenameSynonym.Add("snow, water content", "Snow water equivalent");
            VariablenameSynonym.Add("temperature, air", "Temperature");
            VariablenameSynonym.Add("electrical conductivity milli s", "Electrical conductivity");
            VariablenameSynonym.Add("electrical conductivty milli s", "Electrical conductivity");//splling error in org data
            VariablenameSynonym.Add("precipitation, tipping bucket", "Precipitation");
            VariablenameSynonym.Add("atmospheric pressure", "Barometric pressure");
            VariablenameSynonym.Add("full natural flow", "Streamflow");
            VariablenameSynonym.Add("wind, direction", "Wind direction");
            VariablenameSynonym.Add("wind, speed", "Wind speed");
            VariablenameSynonym.Add("solar radiation", "Radiation, total incoming");
            VariablenameSynonym.Add("flow, river discharge", "Discharge"); 





            VariablenameSynonym.TryGetValue(term, out syn);
            return syn;

        }

        public string ToReadableString(IDbCommand command)
        {
            StringBuilder builder = new StringBuilder();
            if (command.CommandType == CommandType.StoredProcedure)
                builder.AppendLine("Stored procedure: " + command.CommandText);
            else
                builder.AppendLine("Sql command: " + command.CommandText);
            if (command.Parameters.Count > 0)
                builder.AppendLine("With the following parameters.");
            foreach (IDataParameter param in command.Parameters)
            {
                builder.AppendFormat(
                    "     Paramater {0}: {1}",
                    param.ParameterName,
                    (param.Value == null ?
                    "NULL" : param.Value.ToString())).AppendLine();
            }
            return builder.ToString();
        }
    }
}


        
