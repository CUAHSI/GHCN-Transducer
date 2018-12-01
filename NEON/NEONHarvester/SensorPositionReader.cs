using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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

        public List<NeonSensorPosition> ReadSensorPositionsFromUrl(string sensorPositionsUrl, NeonSite site)
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
                                var senPos = new NeonSensorPosition(site);

                                senPos.xOffset = 0;
                                senPos.yOffset = 0;
                                senPos.zOffset = 0;
                                senPos.ReferenceLatitude = site.siteLatitude;
                                senPos.ReferenceLongitude = site.siteLongitude;
                                senPos.ReferenceElevation = 0;

                                for (var index = 0; index < fieldValues.Length; index++)
                                {
                                    string fieldName = fieldNames[index];
                                    var fieldValue = fieldValues[index];
                                    
                                    switch (fieldName)
                                    {                                        
                                        case "HOR.VER":
                                            senPos.HorVerCode = Convert.ToString(fieldValue);
                                            break;
                                        case "xOffset":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.xOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "yOffset":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.yOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "zOffset":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.zOffset = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "referenceLatitude":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.ReferenceLatitude = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "referenceLongitude":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.ReferenceLongitude = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "referenceElevation":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.ReferenceElevation = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "pitch":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.pitch = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "roll":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.roll = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "azimuth":
                                            if (!String.IsNullOrEmpty(Convert.ToString(fieldValue)))
                                            {
                                                senPos.azimuth = Convert.ToDouble(fieldValues[index], CultureInfo.InvariantCulture);
                                            }
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
