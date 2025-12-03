using System.Text.Json.Serialization;
using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class PaymentResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("status")]
    public required string Status { get; set; }
    
    [JsonPropertyName("card_number_last_four")]
    public required string CardNumberLastFour { get; set; }
    
    [JsonPropertyName("expiry_month")]
    public int ExpiryMonth { get; set; }
    
    [JsonPropertyName("expiry_year")]
    public int ExpiryYear { get; set; }
    
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }
    
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}
