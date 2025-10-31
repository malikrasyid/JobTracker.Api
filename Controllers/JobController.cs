using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace JobTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobController : ControllerBase
    {
        private readonly IMongoCollection<JobApplication> _jobs;
        private readonly IMongoCollection<Pipeline> _pipelines;

        public JobController(IMongoClient client, IConfiguration config)
        {
            var db = client.GetDatabase(config["MongoDb:DatabaseName"]);
            _jobs = db.GetCollection<JobApplication>("Jobs");
            _pipelines = db.GetCollection<Pipeline>("Pipelines");
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                                  User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            var userId = GetUserId();
            var jobs = await _jobs.Find(j => j.UserId == userId).ToListAsync();
            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(string id)
        {
            var userId = GetUserId();
            var job = await _jobs.Find(j => j.Id == id && j.UserId == userId).FirstOrDefaultAsync();
            if (job == null)
                return NotFound("Job not found or unauthorized");
            return Ok(job);
        }

        [HttpGet("stage/{stage}")]
        public async Task<IActionResult> GetJobsByStage(string stage)
        {
            var userId = GetUserId();
            var allowedStages = new[] { "Applied", "Interview", "Offer", "Rejected" };
            if (!allowedStages.Contains(stage))
                return BadRequest("Invalid stage.");

            var jobs = await _jobs.Find(j => j.UserId == userId && j.Stage == stage).ToListAsync();
            return Ok(jobs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] JobApplication job)
        {
            var userId = GetUserId();
            job.UserId = userId;
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            // ISSUE 1: Should validate if PipelineId exists and belongs to user
            if (!string.IsNullOrEmpty(job.PipelineId))
            {
                var pipeline = await _pipelines.Find(p => p.Id == job.PipelineId && p.UserId == userId)
                                             .FirstOrDefaultAsync();
                if (pipeline == null)
                    return BadRequest("Invalid pipeline ID or unauthorized");
                
                job.PipelineName = pipeline.Name; // Sync pipeline name
                job.Stage = pipeline.Stages.FirstOrDefault() ?? "Applied"; // Use first stage from pipeline
            }
            else
            {
                // Use default pipeline
                job.PipelineId = JobTracker.Api.Config.DefaultPipeline.Id;
                job.PipelineName = JobTracker.Api.Config.DefaultPipeline.Name;
                job.Stage = JobTracker.Api.Config.DefaultPipeline.Stages[0];
            }

            await _jobs.InsertOneAsync(job);
            return Ok(job);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(string id, [FromBody] JobApplication updated)
        {
            var userId = GetUserId();
            
            // ISSUE 2: Should validate pipeline relationship
            if (!string.IsNullOrEmpty(updated.PipelineId))
            {
                var pipeline = await _pipelines.Find(p => p.Id == updated.PipelineId && p.UserId == userId)
                                             .FirstOrDefaultAsync();
                if (pipeline == null)
                    return BadRequest("Invalid pipeline ID or unauthorized");
                
                // ISSUE 3: Sync pipeline name
                updated.PipelineName = pipeline.Name;
                
                // ISSUE 4: Validate stage exists in pipeline
                if (!pipeline.Stages.Contains(updated.Stage))
                    return BadRequest("Invalid stage for the selected pipeline");
            }

            var filter = Builders<JobApplication>.Filter.Eq(j => j.Id, id) & 
                        Builders<JobApplication>.Filter.Eq(j => j.UserId, userId);

            updated.UpdatedAt = DateTime.UtcNow;
            updated.Id = id;
            updated.UserId = userId;
            var result = await _jobs.ReplaceOneAsync(filter, updated);

            if (result.MatchedCount == 0)
                return NotFound("Job not found or unauthorized");

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            var userId = GetUserId();
            var result = await _jobs.DeleteOneAsync(j => j.Id == id && j.UserId == userId);
            if (result.DeletedCount == 0) return NotFound();
            return Ok();
        }
    }
}
