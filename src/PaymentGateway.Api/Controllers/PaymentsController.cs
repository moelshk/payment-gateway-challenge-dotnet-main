using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> ProcessPaymentAsync([FromBody] PaymentRequest request)
    {
        // Check ModelState validation - if invalid, return BadRequest before calling bank 
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _paymentService.ProcessPaymentAsync(request);
        
        if (response.Status == "Rejected")
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PaymentResponse?> GetPayment(Guid id)
    {
        try
        {
            var payment = _paymentService.GetPayment(id);

            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetPayment: {ex}");
            return StatusCode(500, ex.ToString());
        }
    }
}