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
using System.Reflection;
using System.Text;
using CsvHelper;

namespace USBRHarvester
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

        public void UpdateVariables(List<USBRParameter.Data> parameters)
        {
            var variables = new List<Variable>();
            int unitId;
            string unitName;
            //get Units from ODM
            var odmUnits = GetODMUnits();

            //Get VariableCV from ODM
            var odmVariableCV = GetODMVariableCV();

            //var distinct = (from p in parameters select new { id = p.attributes.parameterTransformation }).ToList().Distinct().ToList();


            //using (var writer = new StreamWriter("c:/temp/parameters.csv"))
            //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            //{
            //    csv.WriteRecords(parameters);
            //}
            //DEBUG:use to filter parameters
            //parameters = parameters.Where(p => p.id.Split('/').Last().Equals("1374")).ToList();
            

            try
            {
                foreach (var p in parameters)
                {
                    //look up and map Variableunits
                    string unitsAbbr = (getUnitSynonym(p.attributes.parameterUnit) != null) ? getUnitSynonym(p.attributes.parameterUnit) : p.attributes.parameterUnit;
                    //_log.LogWrite(String.Format(p.attributes.parameterName + "||" + p.attributes.parameterDescription));
                    
                    //int unitsID = null;//  (int) .FirstOrDefault(x => x.UnitsAbbreviation == unitsAbbr).UnitsID;
                    try
                    {
                         unitId = (from u in odmUnits where u.UnitsAbbreviation.ToLower() == unitsAbbr.ToLower() select u.UnitsID).First();
                         unitName = (from u in odmUnits where u.UnitsAbbreviation.ToLower() == unitsAbbr.ToLower() select u.UnitsName).First();
                        
                    }
                    catch (Exception ex)
                    {
                        _log.LogWrite(String.Format("No match was found: " + p.id.Split('/').Last() + ", " + p.attributes.parameterName + ", " + p.attributes.parameterUnit + ", " + p.attributes.parameterDescription));
                        continue;
                    }
                    //Lookup up and map variablenames
                    //string variableName = (getVariablenameSynonym(p.attributes.parameterName) != null) ? getVariablenameSynonym(p.attributes.parameterName) : p.attributes.parameterName;
                    string variableName = (getVariablenameMapping(p.attributes.parameterName) != null) ? getVariablenameMapping(p.attributes.parameterName) : p.attributes.parameterName;
                    

                    try
                    {
                        var odmvariableName = (from item in odmVariableCV where item.Key.ToLower() == variableName.ToLower() select item.Key).FirstOrDefault();
                        //try to match to term + dissolved  (sometimes the term is a like this "Manganese (Dissolved)" so i match only the first term also need to take care of this case Manganese (Total)

                        if (string.IsNullOrEmpty(odmvariableName))
                        {
                            if (variableName.ToLower().Contains("dissolved") || variableName.ToLower().Contains("total"))//e.g this case "Manganese (Dissolved) and Manganese (Total)" 
                            {
                                if (variableName.ToLower().Contains("dissolved")) { odmvariableName = (from item in odmVariableCV where item.Key.ToLower().Contains(variableName.ToLower().Split(' ').First()) && item.Key.ToLower().Contains("dissolved") select item.Key).FirstOrDefault(); }
                                if (variableName.ToLower().Contains("total")) { odmvariableName = (from item in odmVariableCV where item.Key.ToLower().Contains(variableName.ToLower().Split(' ').First()) && item.Key.ToLower().Contains("total") select item.Key).FirstOrDefault(); }
                            }
                        }
                        if (string.IsNullOrEmpty(odmvariableName))
                        {
                            odmvariableName = (from item in odmVariableCV where item.Key.ToLower().Contains(variableName.ToLower().Split(' ').First()) && item.Key.ToLower().Contains("dissolved")  select item.Key).FirstOrDefault();
                        }
                        //try to match to term + dissolved
                        if (string.IsNullOrEmpty(odmvariableName))
                        {
                            odmvariableName = (from item in odmVariableCV where item.Key.ToLower().StartsWith(variableName.ToLower().Split(' ').First()) && item.Key.ToLower().Contains("total") select item.Key).FirstOrDefault();
                        }

                        if (string.IsNullOrEmpty(odmvariableName))
                        {
                            _log.LogWrite(String.Format("No match was found: " + p.id.Split('/').Last() + ", " + p.attributes.parameterName + ", " + p.attributes.parameterUnit + ", " + p.attributes.parameterDescription));
                            continue;
                        }
                        else //set variable name to new term
                        {
                            variableName = odmvariableName;
                        }
                        //_log.LogWrite(String.Format("SUCCESS:  match was found: " + p.attributes.parameterName));
                    }
                    catch (Exception ex)
                    {
                        //_log.LogWrite(String.Format("No match was found: " + p.id.Split('/').Last() + ", " + p.attributes.parameterName + ", " + p.attributes.parameterUnit + ", " + p.attributes.parameterDescription));
                        _log.LogWrite("UpdateVariables:" + ex.Message);
                       
                    }

                    int timeunitId = 0;
                    int timeSupport = 0;
                    //timesupport
                    //var distinct = (from p in parameters select new { id = p.attributes.parameterTimestep }).ToList().Distinct().ToList();
                    //parameterTimestep options from parameters:  monthly, intermittent, daily, one minute, quarterly, biannually, periodic 
                    switch (p.attributes.parameterTimestep)
                    {
                        case "monthly":                            
                                timeunitId = 106;
                                timeSupport = 1;
                            break;
                        case "daily":
                            timeunitId = 104;
                            timeSupport = 1;
                            break;
                        case "one minute":
                            timeunitId = 102;
                            timeSupport = 1;
                            break;
                        default:
                            timeunitId = 104; 
                            timeSupport = 0;
                            break;

                    }

                    var dataType = string.Empty;

                    switch (p.attributes.parameterTransformation)
                    {
                        case "calculation":
                            dataType = "Unknown";
                            break;
                        case "instant":
                            dataType = "Unknown";
                            break;
                        case "sum":
                            dataType = "Cumulative";
                            break;
                        case "average":
                            dataType = "Average";
                            break;
                        default:
                            dataType = "Unknown";
                            break;

                    }
                    //string timeUnitsName = Convert.ToString(worksheet.Cells[row, 7].Value);
                    //timeUnitsObj = worksheet.Cells[row, 8].Value;
                    //int timeUnitsID = Convert.ToInt32(worksheet.Cells[row, 8].Value);
                    //float timeSupport = float.Parse(Convert.ToString(worksheet.Cells[row, 9].Value), CultureInfo.InvariantCulture);



                    Variable v = new Variable
                    {
                        VariableCode = p.id,
                        VariableName = variableName,
                        VariableUnitsID = unitId,
                        VariableUnitsName = unitName,
                        DataType = dataType,
                    //    SampleMedium = sampleMedium,
                        TimeUnitsID = timeunitId,
                        TimeSupport = timeSupport
                    };
                    variables.Add(v);
                }

            }
            catch (Exception ex)
            {
                _log.LogWrite("UpdateVariables:" + ex.Message);
            }

            _log.LogWrite(String.Format("Found {0} distinct variables.", variables.Count));
            string connString = ConfigurationManager.ConnectionStrings["OdmConnection"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                // to remove any old ("unused") variables
                DeleteOldVariables(connection);

                foreach (Variable variable in variables)
                {
                    try
                    {
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


        private object SaveOrUpdateVariable(Variable variable, SqlConnection connection)
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
                    cmd.Parameters.Add(new SqlParameter("@VariableCode", variable.VariableCode.Split('/').Last()));
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
                    
                    //getcommand text
                    var c = ToReadableString(cmd);
                    cmd.ExecuteNonQuery();
                    variableIDResult = cmd.Parameters["@VariableID"].Value;
                    connection.Close();
                }
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
                    u.UnitsAbbreviation = (string)reader["UnitsAbbreviation"];
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
                    ODMVariableCV.Add( (string)reader["Term"], (string)reader["Definition"]);                    
                }
                cn.Close();
            }

            return ODMVariableCV;
        }

        public string getUnitSynonym(string term)
        {
            string syn = String.Empty;
            //adding synanyms for cv entries missing
            var unitAbbreviationSynonyms = new Dictionary<String, String>();//usbr code, ODM term
            unitAbbreviationSynonyms.Add("af", "ac ft");
            unitAbbreviationSynonyms.Add("Feet per Acre", "ac ft/mo");
            unitAbbreviationSynonyms.Add("pulses/minute", "pulses/min");
            unitAbbreviationSynonyms.Add("Pascal", "pa");
            unitAbbreviationSynonyms.Add("m3/sec", "m^3/s");
            unitAbbreviationSynonyms.Add("count", "#");
            unitAbbreviationSynonyms.Add("count/m3", "#/m^3");
            unitAbbreviationSynonyms.Add("inches", "in");
            unitAbbreviationSynonyms.Add("acres", "ac"); 
            unitAbbreviationSynonyms.Add("kwH", "kW hr");
            unitAbbreviationSynonyms.Add("kw", "kW");
            unitAbbreviationSynonyms.Add("ug/m3", "ug/m^3");


            //syn = unitAbbreviationSynonyms.Where (v => v.Key == term).FirstOrDefault().ToString();
            unitAbbreviationSynonyms.TryGetValue(term, out syn);
            return syn;

        }

        public string getVariablenameSynonym(string term)
        {
            string syn = String.Empty;
            //adding synanyms for cv entries missing
            var VariablenameSynonym = new Dictionary<String, String>();//usbr code, ODM term
            VariablenameSynonym.Add("Water Temperature", "Temperature");

            VariablenameSynonym.TryGetValue(term, out syn);
            return syn;

        }

        public string getVariablenameMapping(string term)
        {

            //set filename from manual mappings!
            string fileName = "USBR Mapping.csv";
            //Get path to file
            string path = AppDomain.CurrentDomain.BaseDirectory + "/Files/" + fileName;
            var VariablenameSynonym = new List<Variablemappings>();
            //var VariablenameSynonym = new Dictionary<String, String>();
            try
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<VariablemappingsMap>();
                    VariablenameSynonym = csv.GetRecords<Variablemappings>().ToList();
                }

            }
            catch (Exception ex)
            {
                _log.LogWrite("Error: read mappings csv: " + ex.Message);
            }


            string syn = String.Empty;
            //adding synanyms for cv entries missing
            //var VariablenameSynonym = new Dictionary<String, String>();//usbr code, ODM term
            //VariablenameSynonym.Add("Water Temperature", "Temperature");

            syn = (from u in VariablenameSynonym where u.USBRVariableName == term select u.CVVariableName).FirstOrDefault();
            if (!string.IsNullOrEmpty(syn))
                return syn;
            else
                return term;

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
