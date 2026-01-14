using SpotRent.Domain.Common;
using SpotRent.Services.Payment;

namespace SpotRent.Services.Interfaces;

public interface IPaymentService
{
    Task<Result<LiqPayPaymentData>>
        CreatePaymentAsync(int id, decimal totalAmount, CancellationToken cancellationToken);

    Task<Result> UpdatePaymentStatusSubscriptionAsync(int subscriptionId, long transactionId, string status,
        CancellationToken cancellationToken);

    Task<Result<LiqPayRefundResponse>> RefundPaymentAsync(int subscriptionId, CancellationToken cancellationToken);

    Task<Result> UpdatePaymentStatusBookingAsync(int bookingId, long transactionId, string status,
        CancellationToken cancellationToken);
}
