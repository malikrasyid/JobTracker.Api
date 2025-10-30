using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobTracker.Api.Models
{
    public class JobApplication
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } 

        [BsonElement("pipelineId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string PipelineId { get; set; }

        [BsonElement("pipelineName")]
        public string PipelineName { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("stage")]
        public string Stage { get; set; } 

        [BsonElement("company")]
        public string Company { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } 

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("source")]
        public string Source { get; set; } = null!;

        [BsonElement("appliedDate")]
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [BsonElement("notes")]
        public string? Notes { get; set; } = null!;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}