using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;

namespace Evolution_Backend.DbModels
{
    [BsonIgnoreExtraElements]
    public class User_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("user_name")]
        public string user_name { get; set; }

        [BsonElement("password")]
        public string password { get; set; }

        [BsonElement("first_name")]
        public string first_name { get; set; }

        [BsonElement("last_name")]
        public string last_name { get; set; }

        [BsonElement("role")]
        public string role { get; set; }

        [BsonElement("status")]
        public int status { get; set; }

        [BsonElement("device_id")]
        public string device_id { get; set; }

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
