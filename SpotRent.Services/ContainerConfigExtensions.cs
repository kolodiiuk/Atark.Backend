using Microsoft.Extensions.DependencyInjection;
using SpotRent.Services.Auth;
using SpotRent.Services.Bookings;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Payment;
using SpotRent.Services.Spaces;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Services;

public static class ContainerConfigExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ISpaceService, SpaceService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<LiqPayHelper, LiqPayHelper>();
    }
}
