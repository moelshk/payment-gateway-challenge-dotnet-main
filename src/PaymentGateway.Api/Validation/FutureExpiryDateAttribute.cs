using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Validation;

public class FutureExpiryDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string expiryDate)
        {
            return new ValidationResult("Expiry date is required.");
        }

        // Parse MM/YYYY format
        var parts = expiryDate.Split('/');
        if (parts.Length != 2)
        {
            return new ValidationResult("Expiry date must be in MM/YYYY format.");
        }

        if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
        {
            return new ValidationResult("Expiry date must contain valid numbers.");
        }

        // Validate month is between 1-12
        if (month < 1 || month > 12)
        {
            return new ValidationResult("Expiry month must be between 1 and 12.");
        }

        // Check if expiry date is in the future
        var now = DateTime.Now;
        var expiryDateTime = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        
        if (expiryDateTime < now)
        {
            return new ValidationResult("Expiry date must be in the future.");
        }

        return ValidationResult.Success;
    }
}
