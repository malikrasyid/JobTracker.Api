using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PipelineController : ControllerBase
    {
        private readonly IMongoCollection<Pipeline> _pipelines;

        public PipelineController(IMongoClient client, IConfiguration config)
        {
            var db = client.GetDatabase(config["MongoDb:DatabaseName"]);
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

        [HttpPost]
        public async Task<IActionResult> CreatePipeline([FromBody]Pipeline pipeline)
        {
            var userId = GetUserId();
            pipeline.UserId = userId;
            await _pipelines.InsertOneAsync(pipeline);
            return Ok(pipeline);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePipeline(string id, [FromBody] Pipeline updated)
        {
            var userId = GetUserId();
            var filter = Builders<Pipeline>.Filter.Eq(p => p.Id, id) &
                        Builders<Pipeline>.Filter.Eq(p => p.UserId, userId);

            updated.Id = id;        // ensure same ID
            updated.UserId = userId; // keep ownership

            var result = await _pipelines.ReplaceOneAsync(filter, updated);

            if (result.MatchedCount == 0)
                return NotFound("Pipeline not found or unauthorized");

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePipeline([FromBody]string id)
        {
            var userId = GetUserId();
            var result = await _pipelines.DeleteOneAsync(p => p.Id == id && p.UserId == userId);
            if (result.DeletedCount == 0) return NotFound();
            return Ok();
        }
    }
}
