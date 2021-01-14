using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;
using System.Collections.Generic;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class PO_Item_Collection
    {

        [BsonElement("item_number")]
        public string item_number { get; set; }

        [BsonElement("item_description")]
        public string item_description { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class PO_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("po_number")]
        public string po_number { get; set; }

        [BsonElement("po_date")]
        public string po_date { get; set; }

        [BsonElement("eta")]
        public string eta { get; set; }

        [BsonElement("etd")]
        public string etd { get; set; }

        [BsonElement("payment_terms")]
        public string payment_terms { get; set; }

        [BsonElement("packing")]
        public string packing { get; set; }

        [BsonElement("ship")]
        public string ship { get; set; }

        [BsonElement("status")]
        public int status { get; set; }

        [BsonElement("total_items")]
        public int total_items { get; set; }

        [BsonElement("total_original_qty")]
        public double total_original_qty { get; set; }

        [BsonElement("total_original_amt")]
        public double total_original_amt { get; set; }

        [BsonElement("total_additional_qty")]
        public double total_additional_qty { get; set; }

        [BsonElement("total_additional_amt")]
        public double total_additional_amt { get; set; }

        [BsonElement("created_by")]
        public string created_by { get; set; }

        [BsonElement("created_on")]
        public DateTime? created_on { get; set; }

        [BsonElement("updated_by")]
        public string updated_by { get; set; }

        [BsonElement("updated_on")]
        public DateTime? updated_on { get; set; }

        [BsonElement("items")]
        public List<PO_Item_Collection> items { get; set; }
    }
}
