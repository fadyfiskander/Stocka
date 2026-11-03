using System.ComponentModel.DataAnnotations;

namespace Stocka.MVC.Areas.Account.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }  = default!;
    }
}