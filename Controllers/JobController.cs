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

            jobUpdate.UpdatedAt = DateTime.UtcNow;
            var result = await _jobs.ReplaceOneAsync(filter, jobUpdate);

            if (result.MatchedCount == 0)
                return NotFound("Job not found or unauthorized");

            return Ok(jobUpdate);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            var userId = GetUserId();
            var result = await _jobs.DeleteOneAsync(j => j.Id == id && jobs.UserId == userId);
            if (result.DeletedCount == 0) return NotFound();
            return Ok();
        }
    }
}
