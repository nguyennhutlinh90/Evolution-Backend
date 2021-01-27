using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System;

namespace Evolution_Backend.DbModels
{
    public class Num_Gen_Collection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("gen_type")]
        public string gen_type { get; set; }

        [BsonElement("gen_prefix")]
        public string gen_prefix { get; set; }

        [BsonElement("gen_length")]
        public int gen_length { get; set; }

        [BsonElement("gen_number")]
        public int gen_number { get; set; }

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
