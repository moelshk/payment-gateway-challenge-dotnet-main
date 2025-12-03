using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Models.Requests;

public class PaymentRequest
{
    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression(@"^\d{14,19}$", ErrorMessage = "Card number must be between 14 and 19 digits and contain only numbers.")]
    public required string CardNumber { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    [RegularExpression(@"^(0?[1-9]|1[0-2])\/\d{4}$", ErrorMessage = "Expiry date must be in MM/YYYY format.")]
    [FutureExpiryDate]
    public required string ExpiryDate { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters.")]
    [AllowedCurrencies]
    public required string Currency { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
    public required string Cvv { get; set; }
}
