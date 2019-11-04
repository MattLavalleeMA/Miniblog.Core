using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models
{
    public class LoginViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}