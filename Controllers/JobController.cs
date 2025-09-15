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

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] JobApplication job)
        {
            var userId = GetUserId();
            job.UserId = userId;
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            var userPipelines = await _pipelines.Find(p => p.UserId == userId).ToListAsync();
            if (userPipelines.Count == 0)
            {
                job.PipelineId = JobTracker.Api.Config.DefaultPipeline.Id;
                job.Stage = JobTracker.Api.Config.DefaultPipeline.Stages[0];
            }
            await _jobs.InsertOneAsync(job);
            return Ok(job);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(string id, [FromBody] JobApplication updated)
        {
            var userId = GetUserId();
            var filter = Builders<JobApplication>.Filter.Eq(j => j.Id, id) & Builders<JobApplication>.Filter.Eq(j => j.UserId, userId);

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
