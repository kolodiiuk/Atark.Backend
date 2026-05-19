using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Logging;

namespace SpotRent.Services.Subscriptions;

public class SubscriptionPlanService : BaseService<SubscriptionPlanService>, ISubscriptionPlanService
{
    public SubscriptionPlanService(SpotRentDbContext context, ILogger<SubscriptionPlanService> logger)
        : base(context, logger)
    {
    }

    public async Task<Result<IEnumerable<SubscriptionPlanDto>>> GetPlansAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plans = await Context.SubscriptionPlans
                .Include(sp => sp.Owner)
                .Where(sp => sp.IsActive == true)
                .Select(sp => new SubscriptionPlanDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Description = sp.Description,
                    Price = sp.Price,
                    Duration = sp.Duration,
                    IncludedHours = sp.IncludedHours,
                    Owner = $"{sp.Owner.FirstName} {sp.Owner.LastName}",
                    IsActive = true,
                    UpdatedAt = sp.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<SubscriptionPlanDto>>(plans);
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlans,
                "DB error retrieving subscription plans. Error: {error}", e.Message);

            return Result.Fail<IEnumerable<SubscriptionPlanDto>>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlans,
                "Error retrieving subscription plans. Error: {error}", e.Message);

            return Result.Fail<IEnumerable<SubscriptionPlanDto>>(
                $"Failure retrieving subscription plans: {e.Message}.");
        }
    }

    public async Task<Result<IEnumerable<SubscriptionPlanDto>>> GetOwnerPlansAsync(
        int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plans = await Context.SubscriptionPlans
                .Where(sp => sp.OwnerId == userId)
                .Select(sp => new SubscriptionPlanDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Description = sp.Description,
                    Price = sp.Price,
                    Duration = sp.Duration,
                    IncludedHours = sp.IncludedHours,
                    OwnerId = userId,
                    IsActive = sp.IsActive,
                    UpdatedAt = sp.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<SubscriptionPlanDto>>(plans);
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlans,
                "DB error retrieving subscription plans. Error: {error}", e.Message);

            return Result.Fail<IEnumerable<SubscriptionPlanDto>>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlans,
                "Error retrieving subscription plans. Error: {error}", e.Message);

            return Result.Fail<IEnumerable<SubscriptionPlanDto>>(
                $"Failure retrieving subscription plans: {e.Message}.");
        }
    }

    public async Task<Result<SubscriptionPlanDto>> GetPlanByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plan = await Context.SubscriptionPlans
                .Include(sp => sp.Owner)
                .Where(sp => sp.Id == id)
                .Select(sp => new SubscriptionPlanDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Description = sp.Description,
                    Price = sp.Price,
                    Duration = sp.Duration,
                    IncludedHours = sp.IncludedHours,
                    OwnerId = sp.OwnerId,
                    Owner = $"{sp.Owner.FirstName} {sp.Owner.LastName}",
                    IsActive = sp.IsActive,
                    UpdatedAt = sp.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (plan is null)
            {
                return Result.Fail<SubscriptionPlanDto>($"No subscription plan with id: {id}");
            }

            return Result.Success(plan);
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlanById,
                "DB error retrieving subscription plan by id {id}. Error: {error}", id, e.Message);

            return Result.Fail<SubscriptionPlanDto>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.GetPlanById,
                "Error retrieving subscription plan by id {id}. Error: {error}", id, e.Message);

            return Result.Fail<SubscriptionPlanDto>(
                $"Failure retrieving subscription plan: {e.Message}.");
        }
    }

    public async Task<Result> CreateSubscriptionPlanAsync(int ownerId, CreateSubscriptionPlanDto subscriptionPlanDto,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plan = new SubscriptionPlan
            {
                Name = subscriptionPlanDto.Name,
                Description = subscriptionPlanDto.Description,
                Price = subscriptionPlanDto.Price,
                Duration = subscriptionPlanDto.Duration,
                IncludedHours = subscriptionPlanDto.IncludedHours,
                IsActive = true,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await Context.AddAsync(plan, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.CreateSubscriptionPlan,
                "DB error creating subscription plan. Error: {error}", e.Message);

            return Result.Fail($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.CreateSubscriptionPlan,
                "Error creating subscription plan. Error: {error}", e.Message);

            return Result.Fail($"Failure creating subscription plan: {e.Message}.");
        }
    }

    public async Task<Result> UpdateSubscriptionPlanAsync(
        int id,
        UpdateSubscriptionPlanDto subscriptionPlanDto,
        int ownerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plan = await Context.SubscriptionPlans.FindAsync(new object[] { id }, cancellationToken);
            if (plan is null)
            {
                return Result.Fail($"No subscription plan with id: {id}");
            }

            plan.UpdatedAt = DateTime.UtcNow;
            plan.Name = subscriptionPlanDto.Name;
            plan.Description = subscriptionPlanDto.Description;
            plan.Price = subscriptionPlanDto.Price;

            Context.Update(plan);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.UpdateSubscriptionPlan,
                "DB error updating subscription plan {id}. Error: {error}", id, e.Message);

            return Result.Fail($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.UpdateSubscriptionPlan,
                "Error updating subscription plan {id}. Error: {error}", id, e.Message);

            return Result.Fail($"Failure updating subscription plan: {e.Message}.");
        }
    }

    public async Task<Result> DeactivateSubscriptionPlanAsync(int subscriptionPlanId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plan = await Context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == subscriptionPlanId,
                cancellationToken);
            if (plan is null)
            {
                return Result.Fail($"No subscription plan with id: {subscriptionPlanId}");
            }

            if (!plan.IsActive)
            {
                return Result.Success();
            }

            plan.IsActive = false;
            Context.Update(plan);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.DeactivateSubscriptionPlan,
                "DB error deactivating subscription plan {subscriptionPlanId}. Error: {error}",
                subscriptionPlanId, e.Message);

            return Result.Fail($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.DeactivateSubscriptionPlan,
                "Error deactivating subscription plan {subscriptionPlanId}. Error: {error}",
                subscriptionPlanId, e.Message);

            return Result.Fail($"Failure deleting subscription plan: {e.Message}.");
        }
    }

    public async Task<Result> DeleteSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var plan = await Context.SubscriptionPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.Id == subscriptionPlanId, cancellationToken);
            if (plan is null)
            {
                return Result.Fail($"No subscription plan with id: {subscriptionPlanId}");
            }

            if (plan.IsActive || plan.Subscriptions.Count != 0)
            {
                return Result.Fail(
                    $"There are active subscriptions on subscription or plan {subscriptionPlanId} is active.");
            }

            Context.Remove(plan);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.DeleteSubscriptionPlan,
                "DB error deleting subscription plan {subscriptionPlanId}. Error: {error}",
                subscriptionPlanId, e.Message);

            return Result.Fail($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionPlanServiceEventIds.DeleteSubscriptionPlan,
                "Error deleting subscription plan {subscriptionPlanId}. Error: {error}",
                subscriptionPlanId, e.Message);

            return Result.Fail($"Failure deleting subscription plan: {e.Message}.");
        }
    }
}
