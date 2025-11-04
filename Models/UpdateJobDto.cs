namespace JobTracker.Api.Models
{
    public class UpdateJobDto
    {
        public string? PipelineId { get; set; }
        public string? PipelineName { get; set; }

        public string? Name { get; set; }
        public string? Stage { get; set; }
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Location { get; set; }

        public string? Source { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string? Notes { get; set; }
    }
}
