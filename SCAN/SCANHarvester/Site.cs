using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCANHarvester
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

        /// <summary>
        /// The 12-digit HUC (based on the Watershed Boundary Dataset) in which the station is located.
        /// </summary>
        public string HUC { get; set; }

        /// <summary>
        /// The 8-digit HUC (based on the Hydrologic Unit Dataset) in which the station is located.
        /// </summary>
        public string HUD { get; set; }

        /// <summary>
        /// The time zone of the actual location of the station 
        /// (Note: This is currently set to the same value as the stationDataTimeZone).
        /// </summary>
        public decimal TimeZone { get; set; }

        /// <summary>
        /// The "acton" id of a station.  This is only used for SNOTEL stations and has also been known as the "CDBS" id.  
        /// (For example, for station '302', the acton id is '17D02S').
        /// </summary>
        public string ActonId { get; set; }

        /// <summary>
        /// The id of the station that is used when sending data to the National Water Service via the SHEF system.
        /// </summary>
        public string ShefId { get; set; }
        
        /// <summary>
        /// The comments value to be inserted into ODM. includes the beginDate, endDate, HUC, HUD, TimeZone, actonId, shefId, stationTriplet
        /// </summary>
        public string Comments
        {
            get
            {
                return String.Format("beginDate={0}|endDate={1}|HUC={2}|HUD={3}|TimeZone={4}|actonId={5}|shefId={6}|stationTriplet={7}|isActive={8}",
                    BeginDate, EndDate, HUC, HUD, TimeZone, ActonId, ShefId, StationTriplet, IsActive);
            }

        }
    }
}
