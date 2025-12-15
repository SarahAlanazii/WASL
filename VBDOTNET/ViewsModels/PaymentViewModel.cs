using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.CompanyVMs
{
    public class PaymentViewModel
    {
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "credit_card";

        [Required(ErrorMessage = "Card number is required")]
        [Display(Name = "Card Number")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Card number must be 16 digits")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must contain only digits")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [Display(Name = "Expiry Date (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Format must be MM/YY")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        [Display(Name = "CVV")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3 or 4 digits")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must contain only digits")]
        public string CVV { get; set; }

        [Required(ErrorMessage = "Card holder name is required")]
        [Display(Name = "Card Holder Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string CardHolder { get; set; }
    }
}
