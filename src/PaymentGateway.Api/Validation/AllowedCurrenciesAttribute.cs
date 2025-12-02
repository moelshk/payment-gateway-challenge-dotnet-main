using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Validation;

public class AllowedCurrenciesAttribute : ValidationAttribute
{
    private static readonly HashSet<string> AllowedCurrencies = new() { "USD", "GBP", "EUR" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string currency)
        {
            return new ValidationResult("Currency is required.");
        }

        if (!AllowedCurrencies.Contains(currency.ToUpper()))
        {
            return new ValidationResult($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");
        }

        return ValidationResult.Success;
    }
}
