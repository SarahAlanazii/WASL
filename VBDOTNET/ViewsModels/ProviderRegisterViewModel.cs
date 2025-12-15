using System.ComponentModel.DataAnnotations;
using Wasl.ViewModels.Validation;

namespace Wasl.ViewModels.Auth
{
    public class ProviderRegisterViewModel : RegisterViewModel
    {
        [Required(ErrorMessage = "Provider name is required")]
        [StringLength(150, ErrorMessage = "Provider name cannot exceed 150 characters")]
        [Display(Name = "Provider/Company Name")]
        public string ProviderName { get; set; }

        [Required(ErrorMessage = "Business registration number is required")]
        [StringLength(100, ErrorMessage = "Registration number cannot exceed 100 characters")]
        [Display(Name = "Business Registration Number")]
        public string BusinessRegistrationNumber { get; set; }

        [Required(ErrorMessage = "Provider address is required")]
        [StringLength(255)]
        [Display(Name = "Address")]
        public string ProviderAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string ProviderCity { get; set; }

        [Required(ErrorMessage = "Region is required")]
        [StringLength(100)]
        [Display(Name = "Region")]
        public string ProviderRegion { get; set; }

        [Required(ErrorMessage = "Service description is required")]
        [StringLength(255, ErrorMessage = "Service description cannot exceed 255 characters")]
        [Display(Name = "Service Description")]
        public string ServiceDescription { get; set; }

        [Required(ErrorMessage = "Provider email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Business Email")]
        public string ProviderEmail { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [PhoneNumber(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string ProviderPhoneNumber { get; set; }
    }
}
