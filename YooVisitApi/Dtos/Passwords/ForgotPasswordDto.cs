using System.ComponentModel.DataAnnotations;

namespace YooVisitApi.Dtos.Passwords
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
