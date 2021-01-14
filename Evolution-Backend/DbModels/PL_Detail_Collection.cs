using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;
using System.Collections.Generic;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class PL_Item_Define_Collection
    {
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

        [BsonElement("item_weight")]
        public double item_weight { get; set; }

        [BsonElement("po_qty")]
        public double po_qty { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PL_Item_Detail_Collection
    {
        [BsonElement("box_number")]
        public string box_number { get; set; }

        [BsonElement("box_dimension")]
        public string box_dimension { get; set; }

        [BsonElement("box_weight")]
        public double box_weight { get; set; }

        [BsonElement("box_status")]
        public int box_status { get; set; }

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

        [BsonElement("item_weight")]
        public double item_weight { get; set; }

        [BsonElement("expected_qty")]
        public double expected_qty { get; set; }

        [BsonElement("packed_qty")]
        public double packed_qty { get; set; }

        [BsonElement("qty_status")]
        public int qty_status { get; set; }

        [BsonElement("note")]
        public string note { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PL_Item_Collection
    {

        [BsonElement("item_number")]
        public string item_number { get; set; }

        [BsonElement("item_description")]
        public string item_description { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class PL_Detail_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("pl_number")]
        public string pl_number { get; set; }

        [BsonElement("po_number")]
        public string po_number { get; set; }

        [BsonElement("customer_code")]
        public string customer_code { get; set; }

        [BsonElement("process_manual")]
        public bool process_manual { get; set; }

        [BsonElement("use_produce_qty")]
        public bool use_produce_qty { get; set; }

        [BsonElement("status")]
        public int status { get; set; }

        [BsonElement("total_boxes")]
        public int total_boxes { get; set; }

        [BsonElement("total_packed_boxes")]
        public int total_packed_boxes { get; set; }

        [BsonElement("total_expected_qty")]
        public double total_expected_qty { get; set; }

        [BsonElement("total_packed_qty")]
        public double total_packed_qty { get; set; }

        [BsonElement("created_by")]
        public string created_by { get; set; }

        [BsonElement("created_on")]
        public DateTime? created_on { get; set; }

        [BsonElement("updated_by")]
        public string updated_by { get; set; }

        [BsonElement("updated_on")]
        public DateTime? updated_on { get; set; }

        [BsonElement("locked_by")]
        public string locked_by { get; set; }

        [BsonElement("locked_on")]
        public DateTime? locked_on { get; set; }

        [BsonElement("items")]
        public List<PL_Item_Collection> items { get; set; }

        [BsonElement("item_definies")]
        public List<PL_Item_Define_Collection> item_definies { get; set; }

        [BsonElement("item_details")]
        public List<PL_Item_Detail_Collection> item_details { get; set; }
    }
}
