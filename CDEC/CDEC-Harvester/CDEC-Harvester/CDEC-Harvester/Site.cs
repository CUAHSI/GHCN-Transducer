using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    public class Site
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        /// <summary>
        /// Elevation of site in meters
        /// </summary>
        public decimal Elevation { get; set; }

        public string SiteName { get; set; }

        /// <summary>
        /// Site ID in the ODM database (calculated automatically upon database row insert)
        /// </summary>
        public long SiteID { get; set; }

        /// <summary>
        /// ODM Site code. The code is formed from the AWDB station triplet by replacing ":" with "_".
        /// The AWDB station triplet is a three-part identifier of the station, in the format stationId:stateCode:networkCode.
        /// </summary>
        public string SiteCode { get; set; }



        /// <summary>
        /// U.S. County name
        /// </summary>
        public string County { get; set; }

        /// <summary>
        /// U.S. State name
        /// </summary>
        public string State { get; set; }      
          
        /// <summary>
        /// The comments value to be inserted into ODM. includes the beginDate, endDate, HUC, HUD, TimeZone, actonId, shefId, stationTriplet
        /// </summary>
        public string Comments { get; set; }
        
    }
}
