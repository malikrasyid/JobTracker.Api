using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobTracker.Api.Models
{
    public class Pipeline
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = "Default Pipeline";

        [BsonElement("stages")]
        public List<string> Stages { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
