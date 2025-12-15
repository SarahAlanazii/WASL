using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.ProviderVMs
{
    public class BidResponseViewModel
    {
        [Required(ErrorMessage = "Bid price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid price must be greater than 0")]
        [Display(Name = "Bid Price (SAR)")]
        public decimal BidPrice { get; set; }

        [Required(ErrorMessage = "Estimated delivery days is required")]
        [Range(1, 365, ErrorMessage = "Estimated days must be between 1 and 365")]
        [Display(Name = "Estimated Delivery Days")]
        public int EstimatedDays { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [Display(Name = "Bid Notes (Optional)")]
        public string BidNotes { get; set; }
    }
}
