using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Models
{
    public class JobDto
    {
        public string? PipelineId { get; set; }

        public string? PipelineName { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Stage { get; set; } = null!;

        [Required]
        public string Company { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        public string Source { get; set; } = "";

        public DateTime? AppliedDate { get; set; }

        public string? Notes { get; set; }
    }
}
