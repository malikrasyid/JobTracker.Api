using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Models
{
    public class PipelineDto
    {
        [Required]
        public string Name { get; set; } = "Default Pipeline";
        
        [Required]
        [MinLength(1, ErrorMessage = "At least one stage is required")]
        public List<string> Stages { get; set; } = new();
    }
}