﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using USBRHarvester;
//
//    var usbrCatalogItem = UsbrCatalogItem.FromJson(jsonString);

namespace USBRHarvester
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class UsbrCatalogItem
    {
        [JsonProperty("links")]
        public Links Links { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("data")]
        public Datum Data { get; set; }
    }

    public partial class Datum
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("relationships")]
        public Relationships Relationships { get; set; }
    }

    public partial class Attributes
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("itemTitle")]
        public string ItemTitle { get; set; }

        [JsonProperty("itemDescription")]
        public string ItemDescription { get; set; }

        [JsonProperty("itemRecordStatusId")]
        public string ItemRecordStatusId { get; set; }

        [JsonProperty("metadataFilePath")]
        public object MetadataFilePath { get; set; }

        [JsonProperty("binaryFilePath")]
        public object BinaryFilePath { get; set; }

        [JsonProperty("isModeled")]
        public bool IsModeled { get; set; }

        [JsonProperty("disclaimer")]
        public string Disclaimer { get; set; }

        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        [JsonProperty("locationSourceCode")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public string LocationSourceCode { get; set; }

        [JsonProperty("parameterSourceCode")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public string ParameterSourceCode { get; set; }

        [JsonProperty("modelNameSourceCode")]
        public object ModelNameSourceCode { get; set; }

        [JsonProperty("temporalStartDate")]
        public string TemporalStartDate { get; set; }

        [JsonProperty("temporalEndDate")]
        public string TemporalEndDate { get; set; }

        [JsonProperty("spatialShortDescription")]
        public object SpatialShortDescription { get; set; }

        [JsonProperty("publicationAuthor")]
        public object PublicationAuthor { get; set; }

        [JsonProperty("publicationEditor")]
        public object PublicationEditor { get; set; }

        [JsonProperty("publicationPublisher")]
        public object PublicationPublisher { get; set; }

        [JsonProperty("publicationPublisherLocation")]
        public object PublicationPublisherLocation { get; set; }

        [JsonProperty("publicationFirstPublicationDate")]
        public object PublicationFirstPublicationDate { get; set; }

        [JsonProperty("publicationPeriodicalName")]
        public object PublicationPeriodicalName { get; set; }

        [JsonProperty("publicationSerialNumber")]
        public object PublicationSerialNumber { get; set; }

        [JsonProperty("publicationDoi")]
        public object PublicationDoi { get; set; }

        [JsonProperty("publicationVolume")]
        public object PublicationVolume { get; set; }

        [JsonProperty("publicationIssue")]
        public object PublicationIssue { get; set; }

        [JsonProperty("publicationSection")]
        public object PublicationSection { get; set; }

        [JsonProperty("publicationStartPage")]
        public object PublicationStartPage { get; set; }

        [JsonProperty("publicationEndPage")]
        public object PublicationEndPage { get; set; }

        [JsonProperty("createDate")]
        public DateTimeOffset CreateDate { get; set; }

        [JsonProperty("dataStructure")]
        public string DataStructure { get; set; }

        [JsonProperty("itemStructureId")]
        public string ItemStructureId { get; set; }

        [JsonProperty("itemType")]
        public ItemType ItemType { get; set; }

        [JsonProperty("matrix")]
        public Matrix Matrix { get; set; }

        [JsonProperty("spatialGeometry")]
        public object SpatialGeometry { get; set; }

        [JsonProperty("spatialResolution")]
        public object SpatialResolution { get; set; }

        [JsonProperty("spatialTransformation")]
        public object SpatialTransformation { get; set; }

        [JsonProperty("spatialOpenDataURL")]
        public object SpatialOpenDataUrl { get; set; }

        [JsonProperty("modelName")]
        public object ModelName { get; set; }

        [JsonProperty("parameterId")]
        public string ParameterId { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("parameterUnit")]
        public string ParameterUnit { get; set; }

        [JsonProperty("parameterTimestep")]
        public string ParameterTimestep { get; set; }

        [JsonProperty("parameterTransformation")]
        public string ParameterTransformation { get; set; }

        [JsonProperty("updateFrequency")]
        public Matrix UpdateFrequency { get; set; }

        [JsonProperty("updateDate")]
        public string UpdateDate { get; set; }
    }

    public partial class ItemType
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("createDate")]
        public DateTimeOffset CreateDate { get; set; }
    }

    public partial class Matrix
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("matrix", NullValueHandling = NullValueHandling.Ignore)]
        public string MatrixMatrix { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("createDate")]
        public DateTimeOffset CreateDate { get; set; }

        [JsonProperty("updatefrequency", NullValueHandling = NullValueHandling.Ignore)]
        public string Updatefrequency { get; set; }
    }

    public partial class Relationships
    {
        [JsonProperty("parameter")]
        public CatalogRecord Parameter { get; set; }

        [JsonProperty("catalogRecord")]
        public CatalogRecord CatalogRecord { get; set; }
    }

    public partial class CatalogRecord
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public partial class Links
    {
        [JsonProperty("self")]
        public string Self { get; set; }
    }

    public partial class Meta
    {
        [JsonProperty("totalItems")]
        public long TotalItems { get; set; }

        [JsonProperty("itemsPerPage")]
        public long ItemsPerPage { get; set; }

        [JsonProperty("currentPage")]
        public long CurrentPage { get; set; }
    }

    public partial class UsbrCatalogItem
    {
        public static UsbrCatalogItem FromJson(string json) => JsonConvert.DeserializeObject<UsbrCatalogItem>(json, USBRHarvester.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this UsbrCatalogItem self) => JsonConvert.SerializeObject(self, USBRHarvester.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}