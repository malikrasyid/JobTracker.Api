using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace JobTracker.Api.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    [Authorize]
    public class JobController : ControllerBase
    {
        private readonly IMongoCollection<JobApplication> _jobs;
        private readonly IMongoCollection<Pipeline> _pipelines;

        public JobController(IMongoClient client, IConfiguration config)
        {
            var mongoDb = Environment.GetEnvironmentVariable("MONGO_DBNAME");
            var db = client.GetDatabase(mongoDb);
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
        public async Task<IActionResult> CreateJob([FromBody] JobDto dto)
        {
            var userId = GetUserId();

            var job = new JobApplication
            {
                UserId = userId,
                Name = dto.Name,
                Company = dto.Company,
                Role = dto.Role,
                Location = dto.Location,
                Source = dto.Source ?? string.Empty,
                AppliedDate = dto.AppliedDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Normalize stage input: treat null/empty/whitespace as not provided
            var requestedStage = string.IsNullOrWhiteSpace(dto.Stage) ? null : dto.Stage.Trim();

            Pipeline? pipeline = null;

            // Case 1: User provided a specific pipeline
            if (!string.IsNullOrEmpty(dto.PipelineId))
            {
                pipeline = await _pipelines.Find(p => p.Id == dto.PipelineId && p.UserId == userId)
                                        .FirstOrDefaultAsync();

                if (pipeline == null)
                    return BadRequest("Invalid pipeline ID or unauthorized.");

                job.PipelineId = pipeline.Id;
                job.PipelineName = pipeline.Name;
            }
            else
            {
                // Case 2: Use the default pipeline
                job.PipelineId = JobTracker.Api.Config.DefaultPipeline.Id;
                job.PipelineName = JobTracker.Api.Config.DefaultPipeline.Name;

                // You may also ensure the default pipeline is inserted into the DB if not exists
                pipeline = new Pipeline
                {
                    Id = JobTracker.Api.Config.DefaultPipeline.Id,
                    Name = JobTracker.Api.Config.DefaultPipeline.Name,
                    Stages = JobTracker.Api.Config.DefaultPipeline.Stages
                };
            }

            // Determine stage (auto-insert first if null or invalid)
            if (string.IsNullOrEmpty(requestedStage))
            {
                job.Stage = pipeline.Stages.FirstOrDefault() ?? "Applied";
            }
            else
            {
                job.Stage = pipeline.Stages.Contains(requestedStage)
                    ? requestedStage
                    : pipeline.Stages.FirstOrDefault() ?? "Applied";
            }

            await _jobs.InsertOneAsync(job);
            return Ok(job);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(string id, [FromBody] JobDto dto)
        {
            var userId = GetUserId();

            var filter = Builders<JobApplication>.Filter.Eq(j => j.Id, id) &
                        Builders<JobApplication>.Filter.Eq(j => j.UserId, userId);

            var existing = await _jobs.Find(filter).FirstOrDefaultAsync();
            if (existing == null)
                return NotFound("Job not found or unauthorized");

            // If pipelineId is provided, validate it belongs to the user
            if (!string.IsNullOrEmpty(dto.PipelineId))
            {
                var pipeline = await _pipelines.Find(p => p.Id == dto.PipelineId && p.UserId == userId)
                                             .FirstOrDefaultAsync();
                if (pipeline == null)
                    return BadRequest("Invalid pipeline ID or unauthorized");

                existing.PipelineId = dto.PipelineId;
                existing.PipelineName = pipeline.Name;

                // Validate stage against pipeline
                if (!string.IsNullOrEmpty(dto.Stage) && !pipeline.Stages.Contains(dto.Stage))
                    return BadRequest("Invalid stage for the selected pipeline");
            }

            // Apply updates (only when provided)
            if (!string.IsNullOrEmpty(dto.Name)) existing.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Stage)) existing.Stage = dto.Stage;
            if (!string.IsNullOrEmpty(dto.Company)) existing.Company = dto.Company;
            if (!string.IsNullOrEmpty(dto.Role)) existing.Role = dto.Role;
            if (!string.IsNullOrEmpty(dto.Location)) existing.Location = dto.Location;
            if (dto.Source != null) existing.Source = dto.Source;
            if (dto.AppliedDate.HasValue) existing.AppliedDate = dto.AppliedDate.Value;
            if (dto.Notes != null) existing.Notes = dto.Notes;

            existing.UpdatedAt = DateTime.UtcNow;

            var result = await _jobs.ReplaceOneAsync(filter, existing);
            if (result.MatchedCount == 0)
                return NotFound("Job not found or unauthorized");

            return Ok(existing);
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
