using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Services.Interfaces;

public interface ISubscriptionService
{
    Task<Result<SubscriptionCreationResponse>> SubscribeAsync(int userId, int subscriptionPlanId,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<SubscriptionDto>>> GetCurrentUserSubscriptionAsync(int userId,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<SubscriptionInfo>>> GetSubscriptionHistoryAsync(int userId,
        CancellationToken cancellationToken);

    Task<Result<Subscription>> GetSubscriptionByIdAsync(int id, int userId, CancellationToken cancellationToken);

    Task<Result> ChangeSubscriptionAsync(int currSubscriptionId, int newPlanId, int userId,
        CancellationToken cancellationToken);

    Task<Result> CancelSubscriptionAsync(int subscriptionId, int userId, CancellationToken cancellationToken);
}
