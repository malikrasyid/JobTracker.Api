using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace JobTracker.Api.Controllers
{
    [ApiController]
    [Route("api/pipelines")]
    [Authorize]
    public class PipelineController : ControllerBase
    {
        private readonly IMongoCollection<Pipeline> _pipelines;

        public PipelineController(IMongoClient client, IConfiguration config)
        {
            var mongoDb = Environment.GetEnvironmentVariable("MONGO_DBNAME");
            var db = client.GetDatabase(mongoDb);
            _pipelines = db.GetCollection<Pipeline>("Pipelines");
        }
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                                  User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        [HttpGet]
        public async Task<IActionResult> GetPipelines()
        {
            var userId = GetUserId();
            var pipelines = await _pipelines.Find(p => p.UserId == userId).ToListAsync();
            if (pipelines.Count == 0)
            {
                return Ok(new List<object> {
                    new {
                        Id = JobTracker.Api.Config.DefaultPipeline.Id,
                        Name = JobTracker.Api.Config.DefaultPipeline.Name,
                        Stages = JobTracker.Api.Config.DefaultPipeline.Stages
                    }
                });
            }
            return Ok(pipelines);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPipelineById(string id) 
        {
            var userId = GetUserId();
            if (id == JobTracker.Api.Config.DefaultPipeline.Id)
            {
                return Ok(new {
                    Id = JobTracker.Api.Config.DefaultPipeline.Id,
                    Name = JobTracker.Api.Config.DefaultPipeline.Name,
                    Stages = JobTracker.Api.Config.DefaultPipeline.Stages
                });
            }
            var pipeline = await _pipelines.Find(p => p.Id == id && p.UserId == userId).FirstOrDefaultAsync();
            if (pipeline == null)
                return NotFound("Pipeline not found or unauthorized");
            return Ok(pipeline);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePipzxeline([FromBody] PipelineDto dto)
        {
            // Validate stages
            if (dto.Stages == null || !dto.Stages.Any())
            {
                return BadRequest(new { message = "At least one stage is required" });
            }

            var userId = GetUserId();
            
            var pipeline = new Pipeline
            {
                Name = dto.Name,
                Stages = [.. dto.Stages], // Create new list to ensure proper copying
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Log the pipeline data before saving
            Console.WriteLine($"Creating pipeline: Name={pipeline.Name}, Stages={string.Join(", ", pipeline.Stages)}");
            
            await _pipelines.InsertOneAsync(pipeline);

            // Verify the saved pipeline
            var saved = await _pipelines.Find(p => p.Id == pipeline.Id).FirstAsync();
            Console.WriteLine($"Saved pipeline: Name={saved.Name}, Stages={string.Join(", ", saved.Stages)}");
            
            return Ok(saved);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePipeline(string id, [FromBody] PipelineDto dto)
        {
            var userId = GetUserId();
            var filter = Builders<Pipeline>.Filter.Eq(p => p.Id, id) &
                        Builders<Pipeline>.Filter.Eq(p => p.UserId, userId);

            var existing = await _pipelines.Find(filter).FirstOrDefaultAsync();
            if (existing == null) return NotFound("Pipeline not found or unauthorized");

            if (dto.Name != null) existing.Name = dto.Name;
            if (dto.Stages != null && dto.Stages.Any()) existing.Stages = new List<string>(dto.Stages);
            existing.UpdatedAt = DateTime.UtcNow;

            var result = await _pipelines.ReplaceOneAsync(filter, existing);
            if (result.MatchedCount == 0) return NotFound("Pipeline not found or unauthorized");

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePipeline(string id)
        {
            var userId = GetUserId();
            var result = await _pipelines.DeleteOneAsync(p => p.Id == id && p.UserId == userId);
            if (result.DeletedCount == 0) return NotFound();
            return Ok();
        }
    }
}
