using Microsoft.Extensions.Logging;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Services.Logging;

internal static class SubscriptionPlanServiceEventIds
{
    internal static readonly EventId GetPlans = new(3101, nameof(SubscriptionPlanService.GetPlansAsync));

    internal static readonly EventId GetPlanById = new(3102, nameof(SubscriptionPlanService.GetPlanByIdAsync));

    internal static readonly EventId CreateSubscriptionPlan = new(3103, nameof(SubscriptionPlanService.CreateSubscriptionPlanAsync));

    internal static readonly EventId UpdateSubscriptionPlan = new(3104, nameof(SubscriptionPlanService.UpdateSubscriptionPlanAsync));

    internal static readonly EventId DeactivateSubscriptionPlan = new(3105, nameof(SubscriptionPlanService.DeactivateSubscriptionPlanAsync));

    internal static readonly EventId ActivateSubscriptionPlan = new(3106, nameof(SubscriptionPlanService.ActivateSubscriptionPlanAsync));

    internal static readonly EventId DeleteSubscriptionPlan = new(3107, nameof(SubscriptionPlanService.DeleteSubscriptionPlanAsync));
}
