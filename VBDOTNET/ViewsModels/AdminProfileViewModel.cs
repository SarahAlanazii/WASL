using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.AdminVMs
{
    public class AdminProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string AdminFirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string AdminLastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string AdminEmail { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string AdminPhoneNumber { get; set; }
    }
}
