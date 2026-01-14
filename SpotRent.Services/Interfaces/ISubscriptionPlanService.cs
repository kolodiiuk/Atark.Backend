using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Services.Interfaces;

public interface ISubscriptionPlanService
{
    Task<Result<IEnumerable<SubscriptionPlanDto>>> GetPlansAsync(CancellationToken cancellationToken);

    Task<Result<SubscriptionPlanDto>> GetPlanByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result> CreateSubscriptionPlanAsync(int ownerId, CreateSubscriptionPlanDto subscriptionPlanDto,
        CancellationToken cancellationToken);

    Task<Result> UpdateSubscriptionPlanAsync(int id, UpdateSubscriptionPlanDto subscriptionPlanDto, int ownerId,
        CancellationToken cancellationToken);

    Task<Result> DeactivateSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken);

    Task<Result> DeleteSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken);
}
