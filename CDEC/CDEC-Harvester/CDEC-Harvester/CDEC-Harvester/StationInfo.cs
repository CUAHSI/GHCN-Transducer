using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    // StationInfo myDeserializedClass = JsonConvert.DeserializeObject<StationInfo>(myJsonResponse); 
    public class SensorNum
    {
        public int sensorNum { get; set; }
        public string name { get; set; }
    }

    public class SensorItem
    {
        public string stationId { get; set; }
        public int sensorNum { get; set; }
        public string durCode { get; set; }
        public object hydrologicArea { get; set; }
        public int sensorId { get; set; }
        public object startDate { get; set; }
        public object endDate { get; set; }
        public string sensorType { get; set; }
        public string sensorLongName { get; set; }
        public string duration { get; set; }
    }

    public class StationInfo
    {
        public List<SensorNum> sensorNums { get; set; }
        public List<SensorItem> sensorItems { get; set; }
    }
}
