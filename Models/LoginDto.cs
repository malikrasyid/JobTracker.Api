using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email or username is required")]
        public string EmailOrUsername { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }
}