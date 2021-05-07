using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using System.Linq;
using System.Reflection;

namespace GldasHarvester
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
                string variablesFile = Path.Combine(executableLocation, "settings", "variables.xlsx");
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
                {
                    rowNum++;
                    if (rowNum == 1)
                    {
                        // Skip header row.
                        continue;
                    }
                    string variableCode = Convert.ToString(worksheet.Cells[row, 1].Value);
                    string variableName = Convert.ToString(worksheet.Cells[row, 2].Value);
                    string sampleMedium = Convert.ToString(worksheet.Cells[row, 3].Value);
                    string dataType = Convert.ToString(worksheet.Cells[row, 4].Value);
                    string unitsName = Convert.ToString(worksheet.Cells[row, 5].Value);
                    int unitsID = Convert.ToInt32(worksheet.Cells[row, 6].Value);


                    Variable v = new Variable
                    {
                        VariableCode = variableCode,
                        VariableName = variableName,
                        VariableUnitsID = unitsID,
                        VariableUnitsName = unitsName,
                        DataType = dataType,
                        SampleMedium = sampleMedium,
                        TimeUnitsID = 103, // hour
                        TimeSupport = 3.0f
                    };
                    variables.Add(v);
                }
            }

            return variables;
        }

    }
}

