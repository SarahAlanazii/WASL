using System.ComponentModel.DataAnnotations;
using Wasl.Models;

namespace Wasl.ViewModels.ProviderVMs
{
    public class ContractSignViewModel
    {
        public Contract Contract { get; set; }

        [Required(ErrorMessage = "Please upload the signed contract document")]
        [Display(Name = "Signed Contract Document (PDF only)")]
        public IFormFile SignedDocument { get; set; }

        [Required(ErrorMessage = "You must accept all terms and conditions")]
        [Display(Name = "I accept all terms and conditions")]
        public bool TermsAccepted { get; set; }
    }
}
