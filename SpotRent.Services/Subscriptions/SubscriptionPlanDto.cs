using SpotRent.Domain.Enums;

namespace SpotRent.Services.Subscriptions;

public class SubscriptionPlanDto
{
    public int Id { get; set; }

    public int IncludedHours { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public Duration Duration { get; set; }

    public int? OwnerId { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }
}
