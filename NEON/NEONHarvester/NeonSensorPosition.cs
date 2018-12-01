using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    public class NeonSensorPosition
    {
        public NeonSensorPosition(NeonSite parentSite)
        {
            ParentSite = parentSite;
            xOffset = 0;
            yOffset = 0;
            zOffset = 0;
            ReferenceElevation = 0;
            ReferenceLatitude = parentSite.siteLatitude;
            ReferenceLongitude = parentSite.siteLongitude;
            pitch = 0;
            roll = 0;
            azimuth = 0;
            neonProductCodes = new List<string>();
        }

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

        public List<string> neonProductCodes { get; set; }
    }
}
