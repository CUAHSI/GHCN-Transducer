using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNOTELHarvester
{
    public class ChangeLog
    {
        public int id { get; set; }
        public int? parentIssueID { get; set; }
        public string issueDate { get; set; }
        public string resolvedDate { get; set; }
        public string dateRangeStart { get; set; }
        public string dateRangeEnd { get; set; }
        public string locationAffected { get; set; }
        public string issue { get; set; }
        public string resolution { get; set; }
    }

    public class Spec
    {
        public int specId { get; set; }
        public string specNumber { get; set; }
    }

    public class NeonSiteInfo
    {
        public string siteCode { get; set; }
        public List<string> availableMonths { get; set; }
        public List<string> availableDataUrls { get; set; }
    }

    public class NeonProduct
    {
        public string productCodeLong { get; set; }
        public string productCode { get; set; }
        public string productCodePresentation { get; set; }
        public string productName { get; set; }
        public string productDescription { get; set; }
        public string productStatus { get; set; }
        public string productCategory { get; set; }
        public bool productHasExpanded { get; set; }
        public string productScienceTeamAbbr { get; set; }
        public string productScienceTeam { get; set; }
        public string productAbstract { get; set; }
        public string productDesignDescription { get; set; }
        public string productStudyDescription { get; set; }
        public string productSensor { get; set; }
        public string productRemarks { get; set; }
        public List<string> themes { get; set; }
        public List<ChangeLog> changeLogs { get; set; }
        public List<Spec> specs { get; set; }
        public List<string> keywords { get; set; }
        public List<NeonSiteInfo> siteCodes { get; set; }
    }

    public class RootObject
    {
        public List<NeonProduct> data { get; set; }
    }
}
