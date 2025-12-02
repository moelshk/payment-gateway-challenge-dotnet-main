using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IBankSimulatorClient _bankClient;
    private readonly PaymentsRepository _repository;

    public PaymentService(IBankSimulatorClient bankClient, PaymentsRepository repository)
    {
        _bankClient = bankClient;
        _repository = repository;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        var bankRequest = new BankPaymentRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate = request.ExpiryDate,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest);

        var status = PaymentStatus.Rejected;
        if (bankResponse != null)
        {
            status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
        }

        var response = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = status.ToString(),
            CardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4),
            ExpiryMonth = int.Parse(request.ExpiryDate.Split('/')[0]),
            ExpiryYear = int.Parse(request.ExpiryDate.Split('/')[1]),
            Currency = request.Currency,
            Amount = request.Amount
        };

        _repository.Add(response);

        return response;
    }

    public PaymentResponse? GetPayment(Guid id)
    {
        return _repository.Get(id);
    }
}
