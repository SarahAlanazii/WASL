using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.CompanyVMs
{
    public class SendRequestViewModel
    {
        [Required(ErrorMessage = "Shipment is required")]
        public int ShipmentId { get; set; }

        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
        public string? Message { get; set; }
    }
}
