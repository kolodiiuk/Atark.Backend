using Microsoft.Extensions.Logging;
using SpotRent.Domain.Common;
using SpotRent.Domain.Enums;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;

namespace SpotRent.Services.Payment;

public class PaymentService : BaseService<PaymentService>, IPaymentService
{
    private readonly LiqPayHelper _liqPayHelper;

    public PaymentService(SpotRentDbContext context, LiqPayHelper liqPayHelper, ILogger<PaymentService> logger)
        : base(context, logger)
    {
        _liqPayHelper = liqPayHelper;
    }

    public async Task<Result<LiqPayPaymentData>> CreatePaymentAsync(int id, decimal total, bool isSubscription,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var paymentData = _liqPayHelper.GeneratePaymentData(
                total,
                "UAH",
                "Desc",
                isSubscription,
                id
            );

            return Result.Success(paymentData);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<LiqPayPaymentData>("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result.Fail<LiqPayPaymentData>($"Failure creating payment: {ex.Message}");
        }
    }

    public async Task<Result> UpdatePaymentStatusSubscriptionAsync(int subscriptionId, long transactionId,
        string status, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = await Context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription is not null)
        {
            switch (status)
            {
                case "sandbox":
                    subscription.PaymentStatus = PaymentStatus.TestPaid;
                    subscription.Status = SubscriptionStatus.Active;
                    break;
                case "success":
                    subscription.PaymentStatus = PaymentStatus.Paid;
                    subscription.Status = SubscriptionStatus.Active;
                    break;
                case "failure" or "error":
                    subscription.PaymentStatus = PaymentStatus.Failed;
                    subscription.Status = SubscriptionStatus.Cancelled;
                    break;
                default:
                    subscription.PaymentStatus = subscription.PaymentStatus;
                    break;
            }

            subscription.TransactionId = transactionId;
            subscription.PaymentProcessedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        return Result.Fail($"No payment for {subscriptionId} is in db");
    }

    public async Task<Result> UpdatePaymentStatusBookingAsync(int bookingId, long transactionId, string status,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booking = await Context.Bookings.FindAsync(new object[] { bookingId }, cancellationToken);
        if (booking is not null)
        {
            switch (status)
            {
                case "sandbox":
                    booking.PaymentStatus = PaymentStatus.TestPaid;
                    booking.Status = BookingStatus.Active;
                    break;
                case "success":
                    booking.PaymentStatus = PaymentStatus.Paid;
                    booking.Status = BookingStatus.Active;
                    break;
                case "failure" or "error":
                    booking.PaymentStatus = PaymentStatus.Failed;
                    booking.Status = BookingStatus.Cancelled;
                    break;
                default:
                    booking.PaymentStatus = booking.PaymentStatus;
                    break;
            }

            booking.TransactionId = transactionId;
            booking.PaymentProcessedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        return Result.Fail($"No payment for {bookingId} is in db");
    }

    public async Task<Result<LiqPayRefundResponse>> RefundPaymentAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await _liqPayHelper.RefundAsync<LiqPayRefundResponse>(id);

            return Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<LiqPayRefundResponse>("Operation cancelled");
        }
        catch (Exception e)
        {
            return Result.Fail<LiqPayRefundResponse>($"Error refunding payment: {e.Message}");
        }
    }
}
