using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class Customer_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("customer_code")]
        public string customer_code { get; set; }

        [BsonElement("customer_name")]
        public string customer_name { get; set; }

        [BsonElement("status")]
        public int status { get; set; }

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
