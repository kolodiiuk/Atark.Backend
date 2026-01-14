using Microsoft.AspNetCore.Mvc;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Payment;
using LiqPayResponse = SpotRent.Services.Payment.LiqPayResponse;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly LiqPayHelper _liqPayHelper;

    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService, LiqPayHelper liqPayHelper)
    {
        _paymentService = paymentService;
        _liqPayHelper = liqPayHelper;
    }

    [HttpPost("callback")]
    [Consumes("application/x-www-form-urlencoded")]
    [EndpointSummary("Processes LiqPay payment callbacks.")]
    [EndpointDescription(
        "Validates the callback signature, decodes the payload, and updates subscription payment status based on LiqPay response data.")]
    public async Task<IActionResult> PaymentCallback([FromForm] LiqPayCallback callback,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_liqPayHelper.VerifyCallback(callback.data, callback.signature))
            {
                return BadRequest("Invalid signature");
            }

            var response = _liqPayHelper.DecodeData<LiqPayResponse>(callback.data);
            var parseResult = int.TryParse(response.order_id, out var paymentId);
            if (parseResult)
            {
                switch (response.status)
                {
                    case "success":
                        await _paymentService.UpdatePaymentStatusSubscriptionAsync(paymentId, response.transaction_id,
                            "Paid", cancellationToken);
                        break;
                    case "failure":
                    case "error":
                        await _paymentService.UpdatePaymentStatusSubscriptionAsync(paymentId, response.transaction_id,
                            "Failed", cancellationToken);
                        break;
                    case "sandbox":
                        await _paymentService.UpdatePaymentStatusSubscriptionAsync(paymentId, response.transaction_id,
                            "TestPaid", cancellationToken);
                        break;
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
