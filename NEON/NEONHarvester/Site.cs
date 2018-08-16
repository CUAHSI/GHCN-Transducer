using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
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
        /// The AWDB station triplet is a three-part identifier of the station, in the format stationId:stateCode:networkCode.
        /// </summary>
        public string StationTriplet { get; set; }

        /// <summary>
        /// U.S. County name
        /// </summary>
        public string County { get; set; }

        /// <summary>
        /// U.S. State name
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The date (yyyy-MM-dd HH:mm:ss) that the station was installed.
        /// </summary>
        public DateTime BeginDate { get; set; }

        /// <summary>
        /// The date (yyyy-MM-dd HH:mm:ss) that the station was discontinued.  
        /// If the station is still active, the end date will be set to 2100-01-01 00:00:00.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// True of the station is still active, false otherwise
        /// </summary>
        public bool IsActive {
            get
            {
                return (EndDate > DateTime.Now);
            }
        }

        public string GetNeonSiteCode()
        {
            // example site code: [PRIN_200.000]
            return SiteCode.Split('_')[0];
        }

        
        /// <summary>
        /// The time zone of the actual location of the station 
        /// (Note: This is currently set to the same value as the stationDataTimeZone).
        /// </summary>
        public decimal TimeZone { get; set; }
 
    }
}
