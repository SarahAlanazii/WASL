using System.ComponentModel.DataAnnotations;
using Wasl.ViewModels.Validation;

namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyProfileViewModel
    {
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string UserEmail { get; set; }

        [Required]
        [Display(Name = "Business Registration Number")]
        public string BusinessRegistrationNumber { get; set; }

        [Required]
        [PhoneNumber]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Region")]
        public string CompanyRegion { get; set; }

        [Required]
        [Display(Name = "City")]
        public string CompanyCity { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string CompanyAddress { get; set; }
    }
}
