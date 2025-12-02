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

        if (!response.IsSuccessStatusCode)
        {
            return null; // Or handle specific status codes like 503
        }

        return await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
    }
}
