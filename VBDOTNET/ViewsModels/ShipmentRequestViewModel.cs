using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.ShipmentVMs
{
    public class ShipmentRequestViewModel
    {
        public int? ShipmentRequestId { get; set; }

        [Required(ErrorMessage = "Goods type is required")]
        [StringLength(100)]
        [Display(Name = "Goods Type")]
        public string GoodsType { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(0.1, 100000, ErrorMessage = "Weight must be between 0.1 and 100000 kg")]
        [Display(Name = "Weight (KG)")]
        public decimal WeightKg { get; set; }

        [Required(ErrorMessage = "Pickup location is required")]
        [StringLength(255)]
        [Display(Name = "Pickup Location")]
        public string PickupLocation { get; set; }

        [Required(ErrorMessage = "Pickup city is required")]
        [Display(Name = "Pickup City")]
        public string PickupCity { get; set; }

        [Required(ErrorMessage = "Pickup region is required")]
        [Display(Name = "Pickup Region")]
        public string PickupRegion { get; set; }

        [Required(ErrorMessage = "Delivery location is required")]
        [StringLength(255)]
        [Display(Name = "Delivery Location")]
        public string DeliveryLocation { get; set; }

        [Required(ErrorMessage = "Delivery city is required")]
        [Display(Name = "Delivery City")]
        public string DeliveryCity { get; set; }

        [Required(ErrorMessage = "Delivery region is required")]
        [Display(Name = "Delivery Region")]
        public string DeliveryRegion { get; set; }

        [Required(ErrorMessage = "Delivery deadline is required")]
        [Display(Name = "Delivery Deadline")]
        [DataType(DataType.DateTime)]
        public DateTime DeliveryDeadline { get; set; }

        [StringLength(255)]
        [Display(Name = "Special Instructions")]
        public string SpecialInstructions { get; set; }
    }
}
