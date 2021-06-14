using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBRHarvester
{
    public class USBRParameter
    { 
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Links
        {
            public string self { get; set; }
            public string first { get; set; }
            public string last { get; set; }
            public string next { get; set; }
        }

        public class Meta
        {
            public int totalItems { get; set; }
            public int itemsPerPage { get; set; }
            public int currentPage { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public int parameterGroupId { get; set; }
            public string parameterName { get; set; }
            public object parameterCode { get; set; }
            public string parameterDescription { get; set; }
            public string parameterTimestep { get; set; }
            public string parameterTransformation { get; set; }
            public string parameterUnit { get; set; }
            public DateTime createDate { get; set; }
            public DateTime updateDate { get; set; }
        }

        public class Data
        {
            public string id { get; set; }
            public string type { get; set; }
            public Attributes attributes { get; set; }
        }

        public class USBRParameterRoot
        {
            public Links links { get; set; }
            public Meta meta { get; set; }
            public List<Data> data { get; set; }
        }
    }

    public class USBRCatalogItem
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Links
        {
            public string self { get; set; }
            public string first { get; set; }
            public string last { get; set; }
            public string next { get; set; }
        }

        public class Meta
        {
            public int totalItems { get; set; }
            public int itemsPerPage { get; set; }
            public int currentPage { get; set; }
        }

        public class ItemType
        {
            public string _id { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class Matrix
        {
            public int _id { get; set; }
            public string matrix { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class SpatialGeometry
        {
            public int _id { get; set; }
            public string spatialGeometry { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class SpatialTransformation
        {
            public int _id { get; set; }
            public string spatialTransformation { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class UpdateFrequency
        {
            public int _id { get; set; }
            public string updatefrequency { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public string itemTitle { get; set; }
            public string itemDescription { get; set; }
            public int itemRecordStatusId { get; set; }
            public object metadataFilePath { get; set; }
            public object binaryFilePath { get; set; }
            public bool isModeled { get; set; }
            public string disclaimer { get; set; }
            public int temporalParameterId { get; set; }
            public string sourceCode { get; set; }
            public string locationSourceCode { get; set; }
            public string parameterSourceCode { get; set; }
            public object modelNameSourceCode { get; set; }
            public string temporalStartDate { get; set; }
            public string temporalEndDate { get; set; }
            public object spatialShortDescription { get; set; }
            public object publicationAuthor { get; set; }
            public object publicationEditor { get; set; }
            public object publicationPublisher { get; set; }
            public object publicationPublisherLocation { get; set; }
            public object publicationFirstPublicationDate { get; set; }
            public object publicationPeriodicalName { get; set; }
            public object publicationSerialNumber { get; set; }
            public object publicationDoi { get; set; }
            public object publicationVolume { get; set; }
            public object publicationIssue { get; set; }
            public object publicationSection { get; set; }
            public object publicationStartPage { get; set; }
            public object publicationEndPage { get; set; }
            public DateTime createDate { get; set; }
            public string fileFormat { get; set; }
            public string dataStructure { get; set; }
            public int itemStructureId { get; set; }
            public ItemType itemType { get; set; }
            public Matrix matrix { get; set; }
            public SpatialGeometry spatialGeometry { get; set; }
            public object spatialResolution { get; set; }
            public SpatialTransformation spatialTransformation { get; set; }
            public object spatialOpenDataURL { get; set; }
            public object modelName { get; set; }
            public UpdateFrequency updateFrequency { get; set; }
            public string updateDate { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
        }

        public class Parameter
        {
            public Data data { get; set; }
        }

        public class CatalogRecord
        {
            public Data data { get; set; }
        }

        public class Relationships
        {
            public Parameter parameter { get; set; }
            public CatalogRecord catalogRecord { get; set; }
        }

        public class USBRCatalogitemRoot
        {
            public Links links { get; set; }
            public Meta meta { get; set; }
            public List<Data> data { get; set; }
        }


    }

    public class USBRCatalogRecord
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Links
        {
            public string self { get; set; }
            public string first { get; set; }
            public string last { get; set; }
            public string next { get; set; }
        }

        public class Meta
        {
            public int totalItems { get; set; }
            public int itemsPerPage { get; set; }
            public int currentPage { get; set; }
        }

        public class SubTheme
        {
            public int _id { get; set; }
            public string subTheme { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class MetadataStandard
        {
            public string _id { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public string recordTitle { get; set; }
            public string recordDescription { get; set; }
            public List<object> programNames { get; set; }
            public int catalogStatusId { get; set; }
            public string generationEffort { get; set; }
            public SubTheme subTheme { get; set; }
            public object metadataFilePath { get; set; }
            public MetadataStandard metadataStandard { get; set; }
            public DateTime createDate { get; set; }
            public DateTime updateDate { get; set; }
            public List<string> tags { get; set; }
            public List<string> themes { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
        }

        public class Location
        {
            public Data data { get; set; }
        }

        public class CatalogItems
        {
            public List<Data> data { get; set; }
        }

        public class Relationships
        {
            public Location location { get; set; }
            public CatalogItems catalogItems { get; set; }
        }

        public class USBRCatalogRecordRoot
        {
            public Links links { get; set; }
            public Meta meta { get; set; }
            public List<Data> data { get; set; }
        }

    }

    // USBRLocationPolygon myDeserializedClass = JsonConvert.DeserializeObject<USBRLocationPolygon>(myJsonResponse); 
    public class USBRLocationPolygon
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class LocationCoordinates
        {
            public string type { get; set; }
            public List<List<List<double>>> coordinates { get; set; }
        }

        public class LocationGeometry
        {
            public int _id { get; set; }
            public string locationGeometry { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class LocationRegion
        {
            public string _id { get; set; }
            public string regionName { get; set; }
            public DateTime createDate { get; set; }
        }

        public class LocationTag
        {
            public int _id { get; set; }
            public string tag { get; set; }
            public DateTime createDate { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public object locationParentId { get; set; }
            public string locationName { get; set; }
            public int locationStatusId { get; set; }
            public LocationCoordinates locationCoordinates { get; set; }
            public object elevation { get; set; }
            public object relatedLocationIds { get; set; }
            public object horizontalDatum { get; set; }
            public LocationGeometry locationGeometry { get; set; }
            public List<string> projectNames { get; set; }
            public string locationTypeName { get; set; }
            public object timezoneName { get; set; }
            public object timezoneOffset { get; set; }
            public object timezone { get; set; }
            public object verticalDatum { get; set; }
            public List<LocationRegion> locationRegions { get; set; }
            public List<LocationTag> locationTags { get; set; }
            public DateTime createDate { get; set; }
            public DateTime updateDate { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
        }

        public class States
        {
            public List<Data> data { get; set; }
        }

        public class LocationUnifiedRegions
        {
            public List<Data> data { get; set; }
        }

        public class CatalogRecords
        {
            public List<Data> data { get; set; }
        }

        public class CatalogItems
        {
            public List<Data> data { get; set; }
        }

        public class ModelRuns
        {
            public List<Data> data { get; set; }
        }

        public class Relationships
        {
            public States states { get; set; }
            public LocationUnifiedRegions locationUnifiedRegions { get; set; }
            public CatalogRecords catalogRecords { get; set; }
            public CatalogItems catalogItems { get; set; }
            public ModelRuns modelRuns { get; set; }
        }

        public class USBRLocationRoot
        {
            public Data data { get; set; }
        }

    }

    public class USBRLocationPoint
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class LocationCoordinates
        {
            public string type { get; set; }
            public List<double> coordinates { get; set; }
        }

        public class HorizontalDatum
        {
            public string _id { get; set; }
            public string definition { get; set; }
        }

        public class LocationGeometry
        {
            public int _id { get; set; }
            public string locationGeometry { get; set; }
            public string definition { get; set; }
            public DateTime createDate { get; set; }
        }

        public class VerticalDatum
        {
            public string _id { get; set; }
            public string definition { get; set; }
        }

        public class LocationRegion
        {
            public string _id { get; set; }
            public string regionName { get; set; }
            public DateTime createDate { get; set; }
        }

        public class LocationTag
        {
            public int _id { get; set; }
            public string tag { get; set; }
            public DateTime createDate { get; set; }
        }

        public class Attributes
        {
            public int _id { get; set; }
            public object locationParentId { get; set; }
            public string locationName { get; set; }
            public int locationStatusId { get; set; }
            public LocationCoordinates locationCoordinates { get; set; }
            public string elevation { get; set; }
            public object relatedLocationIds { get; set; }
            public HorizontalDatum horizontalDatum { get; set; }
            public LocationGeometry locationGeometry { get; set; }
            public List<object> projectNames { get; set; }
            public string locationTypeName { get; set; }
            public string timezoneName { get; set; }
            public int timezoneOffset { get; set; }
            public string timezone { get; set; }
            public VerticalDatum verticalDatum { get; set; }
            public List<LocationRegion> locationRegions { get; set; }
            public List<LocationTag> locationTags { get; set; }
            public DateTime createDate { get; set; }
            public DateTime updateDate { get; set; }
        }

        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
            public Attributes attributes { get; set; }
            public Relationships relationships { get; set; }
        }

        public class States
        {
            public List<Data> data { get; set; }
        }

        public class LocationUnifiedRegions
        {
            public List<Data> data { get; set; }
        }

        public class CatalogRecords
        {
            public List<Data> data { get; set; }
        }

        public class CatalogItems
        {
            public List<Data> data { get; set; }
        }

        public class Relationships
        {
            public States states { get; set; }
            public LocationUnifiedRegions locationUnifiedRegions { get; set; }
            public CatalogRecords catalogRecords { get; set; }
            public CatalogItems catalogItems { get; set; }
        }

        public class USBRLocationPointRoot
        {
            public Data data { get; set; }
        }
    }

    public class ItemLocationParameter
    {
        public string itemId { get; set; }
        public string locationId { get; set; }
        public string parameterId { get; set; }
        public string temporalStartDate { get; set; }
        public string temporalEndDate { get; set; }

        public ItemLocationParameter(string ItemId, string LocationId, string ParameterId, string TemporalStartDate, string TemporalEndDate)
        {
             itemId = ItemId;
             locationId = LocationId;
             parameterId = ParameterId;
             temporalStartDate = TemporalStartDate;
             temporalEndDate = TemporalEndDate;
        }
    }
}
