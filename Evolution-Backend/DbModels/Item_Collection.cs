using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class Item_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("barcode")]
        public string barcode { get; set; }

        [BsonElement("item_number")]
        public string item_number { get; set; }

        [BsonElement("item_description")]
        public string item_description { get; set; }

        [BsonElement("item_group")]
        public string item_group { get; set; }

        [BsonElement("fit")]
        public string fit { get; set; }

        [BsonElement("style")]
        public string style { get; set; }

        [BsonElement("season")]
        public string season { get; set; }

        [BsonElement("quality")]
        public string quality { get; set; }

        [BsonElement("material")]
        public string material { get; set; }

        [BsonElement("color_number")]
        public string color_number { get; set; }

        [BsonElement("color_description")]
        public string color_description { get; set; }

        [BsonElement("size")]
        public string size { get; set; }

        [BsonElement("inseam")]
        public string inseam { get; set; }

        [BsonElement("tariff_number")]
        public string tariff_number { get; set; }

        [BsonElement("created_by")]
        public string created_by { get; set; }

        [BsonElement("created_on")]
        public DateTime? created_on { get; set; }

        [BsonElement("updated_by")]
        public string updated_by { get; set; }

        [BsonElement("updated_on")]
        public DateTime? updated_on { get; set; }
    }
}
