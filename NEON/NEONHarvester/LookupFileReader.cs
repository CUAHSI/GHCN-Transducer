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
    class LookupFileReader
    {
        private LogWriter _log;

        public LookupFileReader(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
        }

        public string LookupFilePath
        {
            get
            {
                // reading the variables from the EXCEL file
                // During "build solution" the EXCEL file is moved to bin/Debug or bin/Release
                string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string variablesFile = Path.Combine(executableLocation, "settings", "neon_variables_lookup.xlsx");
                return variablesFile;
            }
        }

        public List<Variable> ReadVariablesFromExcel()
        {
            // reading the variables from the EXCEL file
            // During "build solution" the EXCEL file is moved to bin/Debug or bin/Release
            //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string variablesFile = Path.Combine(executableLocation, "settings", "neon_variables_lookup.xlsx");
            string variablesFile = this.LookupFilePath;

            var variables = new List<Variable>();
            var methods = new Dictionary<string, MethodInfo>();

            _log.LogWrite("Read Variables from File " + variablesFile);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";

            var variablesFileInfo = new FileInfo(variablesFile);
            using (var package = new ExcelPackage(variablesFileInfo))
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

            return variables;
        }


        public List<string> ReadProductCodesFromExcel()
        {
            // reading the product codes from the EXCEL file
            // During "build solution" the EXCEL file is moved to bin/Debug or bin/Release
            //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string variablesFile = Path.Combine(executableLocation, "settings", "neon_variables_lookup.xlsx");
            string variablesFile = this.LookupFilePath;

            var productCodes = new List<String>();

            _log.LogWrite("Read product codes from File " + variablesFile);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";

            var variablesFileInfo = new FileInfo(variablesFile);
            using (var package = new ExcelPackage(variablesFileInfo))
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
                    if (!productCodes.Contains(productCode))
                    {
                        productCodes.Add(productCode);
                    }
                }
                    
            }

            return productCodes;
        }


        public Dictionary<string, MethodInfo> ReadMethodsFromExcel()
        {
            // reading the methods from the EXCEL file
            // NEON Product code -- CUAHSI Method Code
            // During "build solution" the EXCEL file is moved to bin/Debug or bin/Release
            //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string variablesFile = Path.Combine(executableLocation, "settings", "neon_variables_lookup.xlsx");
            string variablesFile = this.LookupFilePath;

            var variables = new List<Variable>();
            var methods = new Dictionary<string, MethodInfo>();

            _log.LogWrite("Read Methods from File " + variablesFile);
            int rowNum = 0;
            object timeUnitsObj = "timeUnitsObj";

            var variablesFileInfo = new FileInfo(variablesFile);
            using (var package = new ExcelPackage(variablesFileInfo))
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
                    string neonDocument = Convert.ToString(worksheet.Cells[row, 6].Value);


                    MethodInfo newMethod = new MethodInfo()
                    {
                        MethodLink = neonDocument,
                        MethodCode = productCode,
                        MethodDescription = null
                    };

                    if (!methods.ContainsKey(productCode))
                    {
                        methods.Add(productCode, newMethod);
                    }
                }
            }

            return methods;
        }

    }
}
