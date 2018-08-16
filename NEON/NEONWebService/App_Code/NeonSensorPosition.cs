using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    public class NeonSensorPosition
    {
        public string HorVerCode { get; set; }

        public double xOffset { get; set; }

        public double yOffset { get; set; }

        public double zOffset { get; set; }

        public double pitch { get; set; } 

        public double roll { get; set; }

        public double azimuth { get; set; }

        public double ReferenceLatitude { get; set; }

        public double ReferenceLongitude { get; set; }

        public double ReferenceElevation { get; set; }

        public NeonSite ParentSite { get; set; }
    }
}
