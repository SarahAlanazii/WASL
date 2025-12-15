using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Wasl.ViewModels.Validation
{
    public class PhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Use [Required] for mandatory validation
            }

            string phoneNumber = value.ToString();

            // Pattern to accept +9665******** and 05******** formats
            string pattern = @"^((\+9665\d{8})|(05\d{8}))$";

            if (Regex.IsMatch(phoneNumber, pattern))
            {
                return ValidationResult.Success;
            }

            string displayName = validationContext.DisplayName ?? validationContext.MemberName;
            return new ValidationResult($"{displayName} must be a valid phone number. Example: +966566193395 or 0566193395");
        }
    }
}
