using System.Collections.Generic;

namespace NEONHarvester
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

    public class NeonProductCollection
    {
        public List<NeonProduct> data { get; set; }
    }

    public class NeonProductData
    {
        public NeonProduct data { get; set; }
    }

    public class NeonProductInfo
    {
        public string dataProductCode { get; set; }
        public string dataProductTitle { get; set; }
        public List<string> availableMonths { get; set; }
        public List<string> availableDataUrls { get; set; }
    }


    public class NeonSite
    {
        public string siteDescription { get; set; }
        public double siteLongitude { get; set; }
        public string siteType { get; set; }
        public string stateName { get; set; }
        public string stateCode { get; set; }
        public double siteLatitude { get; set; }
        public string domainName { get; set; }
        public string domainCode { get; set; }
        public string siteCode { get; set; }
        public List<NeonProductInfo> dataProducts { get; set; }
        public string siteName { get; set; }
    }

    public class NeonSiteCollection
    {
        public List<NeonSite> data { get; set; }
    }

    public class NeonSiteItem
    {
        public NeonSite data { get; set; }
    }

    public class NeonFile
    {
        public string crc32 { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public string url { get; set; }
    }

    public class NeonFileCollection
    {
        public List<NeonFile> files { get; set; }
    }

    public class NeonFileData
    {
        public NeonFileCollection data { get; set; }
    }
}
