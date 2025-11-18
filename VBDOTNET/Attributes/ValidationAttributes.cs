using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Wasl.Attributes
{
    /// <summary>
    /// Validates Saudi phone numbers in formats: +9665XXXXXXXX or 05XXXXXXXX
    /// </summary>
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

    /// <summary>
    /// Validates Saudi National ID (10 digits with valid region code)
    /// </summary>
    public class SaudiNationalIdAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Use [Required] for mandatory validation
            }

            string nationalId = value.ToString();

            // Ensure the National ID has the correct length
            if (nationalId.Length != 10)
            {
                return new ValidationResult($"The {validationContext.DisplayName ?? validationContext.MemberName} must be exactly 10 digits.");
            }

            // Extract the region or city code
            char regionCode = nationalId[0];

            // Validate the region or city code
            char[] validRegionCodes = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            if (!Array.Exists(validRegionCodes, code => code == regionCode))
            {
                return new ValidationResult($"The {validationContext.DisplayName ?? validationContext.MemberName} has an invalid region code.");
            }

            // Check if all characters are digits
            if (!nationalId.All(char.IsDigit))
            {
                return new ValidationResult($"The {validationContext.DisplayName ?? validationContext.MemberName} must contain only digits.");
            }

            return ValidationResult.Success;
        }
    }
}