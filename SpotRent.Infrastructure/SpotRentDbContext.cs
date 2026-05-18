using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpotRent.Domain.Entities;
using SpotRent.Infrastructure.Extensions;

namespace SpotRent.Infrastructure;

public class SpotRentDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public SpotRentDbContext(DbContextOptions<SpotRentDbContext> options) : base(options)
    {
    }

    public DbSet<AccessLog> AccessLogs { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Device> Devices { get; set; }

    public DbSet<Space> Spaces { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

    public DbSet<WorkingHours> WorkingHours { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseSnakeCaseNamingConvention();
        builder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(typeof(SpotRentDbContext)));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=atark;Username=myuser;Password=mypassword;");
        }
    }
}
