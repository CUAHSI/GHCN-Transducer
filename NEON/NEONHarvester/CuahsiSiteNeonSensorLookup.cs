using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    /// <summary>
    /// A lookup dictionary {CUAHSI SiteCode -> NeonSensorPosition object}
    /// Use this lookup for quick access to NEON sensor by CUAHSI SiteCode.
    /// The CUAHSI site code is composed of "[NEON]_[HOR.VER]" where:
    /// [NEONSITE] is a 4-letter NEON site code, e.g. ARIK
    /// [HOR.VER] is a site-specific NEON sensor index, e.g. 002.503
    /// The NeonSensorPosition object contains a ParentSite property,
    /// allowing access to NEON's parent site object which includes data availability
    /// information.
    /// </summary>
    class CuahsiSiteNeonSensorLookup
    {
        public CuahsiSiteNeonSensorLookup()
        {
            Lookup = new Dictionary<string, NeonSensorPosition>();
        }

        public Dictionary<string, NeonSensorPosition> Lookup { get; set; }
    }
}
