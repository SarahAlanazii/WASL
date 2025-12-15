using System.ComponentModel.DataAnnotations;
using Wasl.ViewModels.Validation;

namespace Wasl.ViewModels.Auth
{
    public class CompanyRegisterViewModel : RegisterViewModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(150, ErrorMessage = "Company name cannot exceed 150 characters")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Business registration number is required")]
        [StringLength(100, ErrorMessage = "Registration number cannot exceed 100 characters")]
        [Display(Name = "Business Registration Number")]
        public string BusinessRegistrationNumber { get; set; }

        [Required(ErrorMessage = "Company address is required")]
        [StringLength(255)]
        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string CompanyCity { get; set; }

        [Required(ErrorMessage = "Region is required")]
        [StringLength(100)]
        [Display(Name = "Region")]
        public string CompanyRegion { get; set; }

        [Required(ErrorMessage = "Company email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Company Email")]
        public string CompanyEmail { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [PhoneNumber(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
