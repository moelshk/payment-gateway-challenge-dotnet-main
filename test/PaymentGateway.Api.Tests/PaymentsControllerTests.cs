using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    #region GET /api/payments/{id} Tests

    [Fact]
    public async Task GetPayment_RetrievesPaymentSuccessfully()
    {
        // Arrange
        var payment = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2025, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP",
            Status = "Authorized"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(paymentsRepository);
                services.AddScoped<IBankSimulatorClient>(sp => new Mock<IBankSimulatorClient>().Object);
                services.AddScoped<IPaymentService>(sp => new PaymentService(
                    sp.GetRequiredService<IBankSimulatorClient>(),
                    paymentsRepository));
            }))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Status code: {response.StatusCode}, Content: {content}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = System.Text.Json.JsonSerializer.Deserialize<PaymentResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse!.Id);
        Assert.Equal(payment.Status, paymentResponse.Status);
    }

    [Fact]
    public async Task GetPayment_Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IBankSimulatorClient>(sp => new Mock<IBankSimulatorClient>().Object);
            }))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/payments - Authorized Tests

    [Fact]
    public async Task ProcessPayment_CardEndingInOddNumber_ReturnsAuthorized()
    {
        // Arrange
        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "auth-code-123"
        };

        var mockBankClient = new Mock<IBankSimulatorClient>();
        mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IBankSimulatorClient>(sp => mockBankClient.Object);
            }))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123451", // Ends in 1 (odd)
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Authorized", paymentResponse!.Status);
        Assert.Equal("3451", paymentResponse.CardNumberLastFour);
        Assert.Equal(12, paymentResponse.ExpiryMonth);
        Assert.Equal(2025, paymentResponse.ExpiryYear);
        Assert.Equal("USD", paymentResponse.Currency);
        Assert.Equal(100, paymentResponse.Amount);
    }

    #endregion

    #region POST /api/payments - Declined Tests

    [Fact]
    public async Task ProcessPayment_CardEndingInEvenNumber_ReturnsDeclined()
    {
        // Arrange
        var bankResponse = new BankPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = ""
        };

        var mockBankClient = new Mock<IBankSimulatorClient>();
        mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IBankSimulatorClient>(sp => mockBankClient.Object);
            }))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456", // Ends in 6 (even)
            ExpiryDate = "12/2025",
            Currency = "GBP",
            Amount = 500,
            Cvv = "456"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Declined", paymentResponse!.Status);
    }

    #endregion

    #region POST /api/payments - Rejected (Validation) Tests

    [Fact]
    public async Task ProcessPayment_InvalidCardNumber_ReturnsRejected()
    {
        // Arrange
        var client = new WebApplicationFactory<PaymentsController>().CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "123", // Too short
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_ExpiredCard_ReturnsRejected()
    {
        // Arrange
        var client = new WebApplicationFactory<PaymentsController>().CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "01/2020", // Expired
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_InvalidCurrency_ReturnsRejected()
    {
        // Arrange
        var client = new WebApplicationFactory<PaymentsController>().CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "12/2025",
            Currency = "XYZ", // Invalid currency
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_InvalidAmount_ReturnsRejected()
    {
        // Arrange
        var client = new WebApplicationFactory<PaymentsController>().CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 0, // Invalid amount
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_InvalidCvv_ReturnsRejected()
    {
        // Arrange
        var client = new WebApplicationFactory<PaymentsController>().CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 100,
            Cvv = "12" // Too short
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_InvalidInput_DoesNotCallBank()
    {
        // Arrange - This test verifies that the bank is NOT called when validation fails
        var mockBankClient = new Mock<IBankSimulatorClient>();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IBankSimulatorClient>(sp => mockBankClient.Object);
            }))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "123", // Invalid - too short
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Verify that the bank client was NEVER called (requirement: don't call bank if input is invalid)
        mockBankClient.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never);
    }

    #endregion

    #region POST /api/payments - Bank Error Tests

    [Fact]
    public async Task ProcessPayment_BankReturnsError_ReturnsRejected()
    {
        // Arrange
        var mockBankClient = new Mock<IBankSimulatorClient>();
        mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync((BankPaymentResponse?)null); // Bank error (503, etc.)

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IBankSimulatorClient>(sp => mockBankClient.Object);
            }))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123450", // Ends in 0 (bank error)
            ExpiryDate = "12/2025",
            Currency = "EUR",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Rejected", paymentResponse!.Status);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task ProcessPayment_MinimumCardLength_Succeeds()
    {
        // Arrange
        var bankResponse = new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" };
        var mockBankClient = new Mock<IBankSimulatorClient>();
        mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var client = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddScoped<IBankSimulatorClient>(_ => mockBankClient.Object)))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "12345678901234", // 14 digits (minimum)
            ExpiryDate = "12/2025",
            Currency = "USD",
            Amount = 1, // Minimum amount
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_MaximumCardLength_Succeeds()
    {
        // Arrange
        var bankResponse = new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" };
        var mockBankClient = new Mock<IBankSimulatorClient>();
        mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var client = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddScoped<IBankSimulatorClient>(_ => mockBankClient.Object)))
            .CreateClient();

        var request = new PaymentRequest
        {
            CardNumber = "1234567890123456789", // 19 digits (maximum)
            ExpiryDate = "12/2025",
            Currency = "GBP",
            Amount = int.MaxValue, // Maximum amount
            Cvv = "1234" // 4 digits CVV
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_AllSupportedCurrencies_Succeed()
    {
        // Test USD, GBP, EUR
        var currencies = new[] { "USD", "GBP", "EUR" };
        
        foreach (var currency in currencies)
        {
            // Arrange
            var bankResponse = new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" };
            var mockBankClient = new Mock<IBankSimulatorClient>();
            mockBankClient.Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync(bankResponse);

            var client = new WebApplicationFactory<PaymentsController>()
                .WithWebHostBuilder(b => b.ConfigureServices(s => s.AddScoped<IBankSimulatorClient>(_ => mockBankClient.Object)))
                .CreateClient();

            var request = new PaymentRequest
            {
                CardNumber = "1234567890123451",
                ExpiryDate = "12/2025",
                Currency = currency,
                Amount = 100,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    #endregion
}