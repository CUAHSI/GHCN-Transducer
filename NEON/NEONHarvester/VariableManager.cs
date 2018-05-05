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
using Newtonsoft.Json;

namespace NEONHarvester
{
    class ProductInfo
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ProductStatus { get; set; }
        public int NumSites { get; set; }
        public int NumMonths { get; set; }
    }


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


        public NeonProductCollection ReadProductsFromApi()
        {
            var neonProducts = new NeonProductCollection();
            try
            {
                string url = "http://data.neonscience.org/api/v0/products";
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    var jsonData = client.DownloadString(url);

                    neonProducts = JsonConvert.DeserializeObject<NeonProductCollection>(jsonData);
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("ReadSitesFromApi ERROR: " + ex.Message);
            }
            return (neonProducts);
        }


        public void WriteProductTable()
        {
            var products = ReadProductsFromApi();

            var productInfos = new List<ProductInfo>();
            foreach (var prod in products.data)
            {
                var p = new ProductInfo();
                p.ProductCode = prod.productCode;
                p.ProductName = prod.productName;
                p.ProductStatus = prod.productStatus;
                if (prod.siteCodes is null)
                {
                    p.NumSites = 0;
                    p.NumMonths = 0;
                }
                else
                {
                    p.NumSites = prod.siteCodes.Count;
                    p.NumMonths = 0;
                    foreach (var sc in prod.siteCodes)
                    {
                        foreach (var m in sc.availableMonths)
                        {
                            p.NumMonths += 1;
                        }
                    }
                }
                productInfos.Add(p);
            }

            var file = new FileInfo("neon_products.xlsx");
            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("test");

                worksheet.Cells["A1"].LoadFromCollection(productInfos, true, OfficeOpenXml.Table.TableStyles.Medium1);

                package.Save();
            }
        }


        public void UpdateVariables()
        {
            // reading the variables from the EXCEL file
            // During "build solution" the EXCEL file is moved to bin/Debug or bin/Release
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string variablesFile = Path.Combine(executableLocation, "settings", "neon_variables_lookup_example.xlsx");

            var variables = new List<Variable>();
            var methods = new Dictionary<string, MethodInfo>();

            _log.LogWrite("Read Variables from File " + variablesFile);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";
            try
            {
                var variablesFileInfo = new FileInfo(variablesFile);
                using(var package = new ExcelPackage(variablesFileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.First();
                    var start = worksheet.Dimension.Start;
                    var end = worksheet.Dimension.End;
                    for (int row = start.Row; row <= end.Row; row++)
                    { // Row by row..
                        rowNum++;
                        string productCode = Convert.ToString(worksheet.Cells[row, 1].Value);
                        if (productCode == "ProductCode")
                        {
                            continue;
                        }
                        string used = Convert.ToString(worksheet.Cells[row, 7].Value);
                        if (used != "yes")
                        {
                            continue;
                        }
                        string name = Convert.ToString(worksheet.Cells[row, 2].Value);
                        string productStatus = Convert.ToString(worksheet.Cells[row, 3].Value);
                        string neonTable = Convert.ToString(worksheet.Cells[row, 4].Value);
                        string neonAttribute = Convert.ToString(worksheet.Cells[row, 5].Value);
                        string neonDocument = Convert.ToString(worksheet.Cells[row, 6].Value);
                        string cuahsiVariableCode = Convert.ToString(worksheet.Cells[row, 8].Value);
                        string cuahsiVariableName = Convert.ToString(worksheet.Cells[row, 9].Value);
                        string generalCategory = Convert.ToString(worksheet.Cells[row, 10].Value);
                        string sampleMedium = Convert.ToString(worksheet.Cells[row, 11].Value);
                        string dataType = Convert.ToString(worksheet.Cells[row, 12].Value);
                        string unitsName = Convert.ToString(worksheet.Cells[row, 13].Value);
                        int unitsID = Convert.ToInt32(worksheet.Cells[row, 14].Value);
                        
                        Variable v = new Variable
                        {
                            VariableCode = cuahsiVariableCode,
                            VariableName = cuahsiVariableName,
                            VariableUnitsID = unitsID,
                            VariableUnitsName = unitsName,
                            DataType = dataType,
                            GeneralCategory = generalCategory,
                            SampleMedium = sampleMedium,
                            TimeUnitsID = 102, // minute
                            TimeSupport = 30.0f
                        };
                        variables.Add(v);
                    }
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
