using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;

namespace Evolution_Backend.DbModels
{
    public class Action_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("action_type")]
        public int action_type { get; set; }

        [BsonElement("action_content")]
        public string action_content { get; set; }

        [BsonElement("created_by")]
        public string created_by { get; set; }

        [BsonElement("created_on")]
        public DateTime? created_on { get; set; }
    }
}
