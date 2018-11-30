using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace NEONHarvester
{
    class SensorPositionReader
    {
        private LogWriter _log;

        public SensorPositionReader(LogWriter log)
        {
            // initialize the logger and the variable lookup
            _log = log;
        }

        public List<NeonSensorPosition> ReadSensorPositionsFromUrl(string sensorPositionsUrl)
        {
            var senPosList = new List<NeonSensorPosition>();
            try
            {
                var client = new WebClient();
                using (var stream = client.OpenRead(sensorPositionsUrl))
                {

                    using (TextFieldParser parser = new TextFieldParser(stream))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");
                        int lineNo = 0;
                        var fieldNames = new string[10];
                        while (!parser.EndOfData)
                        {
                            lineNo += 1;

                            if (lineNo == 1)
                            {
                                fieldNames = parser.ReadFields();
                            }
                            else
                            {
                                string[] fieldValues = parser.ReadFields();
                                var senPos = new NeonSensorPosition();
                                for (var index = 0; index < fieldValues.Length; index++)
                                {
                                    string fieldName = fieldNames[index];
                                    switch (fieldName)
                                    {
                                        case "HOR.VER":
                                            senPos.HorVerCode = fieldValues[index];
                                            break;
                                        case "xOffset":
                                            senPos.xOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;
                                        case "yOffset":
                                            senPos.yOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;
                                        case "zOffset":
                                            senPos.zOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;
                                        case "referenceLatitude":
                                            senPos.ReferenceLatitude = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;
                                        case "referenceLongitude":
                                            senPos.ReferenceLongitude = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;
                                        case "referenceElevation":
                                            senPos.ReferenceElevation = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            break;

                                    }
                                }
                                senPosList.Add(senPos);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWrite("Location cannot be harvested. Reasson: SensorPositionReader ERROR for url: " + sensorPositionsUrl + " " + ex.Message);
            }
            return senPosList;
        }
    }
}
