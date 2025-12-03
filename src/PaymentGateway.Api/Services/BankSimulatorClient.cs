using System.Net;
using System.Text.Json;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class BankSimulatorClient : IBankSimulatorClient
{
    private readonly HttpClient _httpClient;

    public BankSimulatorClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BankPaymentResponse?> ProcessPaymentAsync(BankPaymentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request);

        // Handle different status codes from the bank simulator
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable) // 503
        {
            // Bank simulator returned 503 (card ending in 0) - return null to indicate error
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest) // 400
        {
            // Bank simulator returned 400 (missing required fields) - return null
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            // Any other non-success status code - return null
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
    }
}
