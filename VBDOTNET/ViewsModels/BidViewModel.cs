using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.BidVMs
{
    public class BidViewModel
    {
        public int ShipmentRequestId { get; set; }

        [Required(ErrorMessage = "Bid price is required")]
        [Range(1, 1000000, ErrorMessage = "Bid price must be between 1 and 1,000,000")]
        [Display(Name = "Bid Price (SAR)")]
        public decimal BidPrice { get; set; }

        [Required(ErrorMessage = "Estimated delivery days is required")]
        [Range(1, 365, ErrorMessage = "Estimated days must be between 1 and 365")]
        [Display(Name = "Estimated Delivery Days")]
        public int EstimatedDeliveryDays { get; set; }

        [StringLength(255)]
        [Display(Name = "Notes/Comments")]
        public string BidNotes { get; set; }
    }
}
