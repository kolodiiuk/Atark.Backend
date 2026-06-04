using SpotRent.Api.Controllers;

namespace SpotRent.Api.Logging;

internal static class SubscriptionPlanControllerEventIds
{
    internal static readonly EventId UpdateSubscriptionPlanEvent = new(3001, nameof(SubscriptionPlanController.UpdateSubscriptionPlanAsync));
    
    internal static readonly EventId DeleteSubscriptionPlanEvent = new(3002, nameof(SubscriptionPlanController.DeleteSubscriptionPlanAsync));
    
    internal static readonly EventId DeactivateSubscriptionPlanEvent = new(3003, nameof(SubscriptionPlanController.DeactivateSubscriptionPlanAsync));

    internal static readonly EventId ActivateSubscriptionPlanEvent = new(3004, nameof(SubscriptionPlanController.ActivateSubscriptionPlanAsync));
}
