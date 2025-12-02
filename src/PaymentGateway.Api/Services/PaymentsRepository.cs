using System.Collections.Concurrent;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, PaymentResponse> _payments = new();

    public void Add(PaymentResponse payment)
    {
        _payments.TryAdd(payment.Id, payment);
    }

    public PaymentResponse? Get(Guid id)
    {
        _payments.TryGetValue(id, out var payment);
        return payment;
    }
}