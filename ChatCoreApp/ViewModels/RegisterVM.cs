using System.ComponentModel.DataAnnotations;

namespace ChatCoreApp.ViewModels
{
    public class RegisterVM
    {
        [Required]
        public string UserName { get; set; }

        [Required]

        [EmailAddress]

        public string Email { get; set; }

        [Required]

        [DataType(DataType.Password)]

        public string Password { get; set; }

        [Compare("Password")]

        public string ConfirmPassword { get; set; }
    }
}
