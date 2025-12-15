using System.ComponentModel.DataAnnotations;
using Wasl.ViewModels.Validation;

namespace Wasl.ViewModels.ProviderVMS
{
    public class ProviderProfileViewModel
    {
        [Required]
        [Display(Name = "Provider Name")]
        public string ProviderName { get; set; }

        [Required]
        [Display(Name = "Business Registration Number")]
        public string BusinessRegistrationNumber { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string ProviderAddress { get; set; }

        [Required]
        [Display(Name = "Region")]
        public string ProviderRegion { get; set; }

        [Required]
        [Display(Name = "City")]
        public string ProviderCity { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Service Description")]
        public string ServiceDescription { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string ProviderEmail { get; set; }

        [Required]
        [PhoneNumber]
        [Display(Name = "Phone Number")]
        public string ProviderPhoneNumber { get; set; }
    }
}
