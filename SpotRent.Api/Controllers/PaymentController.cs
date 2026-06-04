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

            var id = response.order_id.Substring(1);
            if (response.order_id[0] == 's')
            {
                var subParseRes = int.TryParse(id, out var subId);
                if (!subParseRes)
                {
                    return BadRequest();
                }

                var res = await _paymentService.UpdatePaymentStatusSubscriptionAsync(
                    subId,
                    response.transaction_id,
                    response.status,
                    cancellationToken);

                if (res.Failure)
                {
                    return StatusCode(418);
                }
            }
            else if (response.order_id[0] == 'b')
            {
                var bookingParseRes = int.TryParse(id, out var bookingId);
                if (!bookingParseRes)
                {
                    return BadRequest();
                }

                var res = await _paymentService.UpdatePaymentStatusBookingAsync(
                    bookingId,
                    response.transaction_id,
                    response.status,
                    cancellationToken);
                if (res.Failure)
                {
                    return StatusCode(418);
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
