using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class PO_Detail_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("po_number")]
        public string po_number { get; set; }

        [BsonElement("item_number")]
        public string item_number { get; set; }

        [BsonElement("item_description")]
        public string item_description { get; set; }

        [BsonElement("color_number")]
        public string color_number { get; set; }

        [BsonElement("color_description")]
        public string color_description { get; set; }

        [BsonElement("inseam")]
        public string inseam { get; set; }

        [BsonElement("size")]
        public string size { get; set; }

        [BsonElement("barcode")]
        public string barcode { get; set; }

        [BsonElement("original_qty")]
        public double original_qty { get; set; }

        [BsonElement("additional_qty")]
        public double additional_qty { get; set; }

        [BsonElement("price")]
        public double price { get; set; }

        [BsonElement("tariff_number")]
        public string tariff_number { get; set; }

        [BsonElement("quality")]
        public string quality { get; set; }

        [BsonElement("material")]
        public string material { get; set; }
    }
}
