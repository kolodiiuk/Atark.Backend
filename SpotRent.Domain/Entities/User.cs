using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SpotRent.Domain.Enums;

namespace SpotRent.Domain.Entities;

public class User : IdentityUser<int>
{
    [Required]
    public string FirstName { get; set; }
        
    [Required]
    public string LastName { get; set; }
        
    [Required]
    public Role Role { get; set; } = Role.User;

    public string GoogleId { get; set; }

    public string PictureUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Space> Spaces { get; set; } = new List<Space>();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<UserRefreshToken> RefreshTokens { get; set; }
}
