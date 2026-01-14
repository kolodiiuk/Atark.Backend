using System.Linq.Expressions;
using System.Transactions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Domain.Extensions;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Logging;
using SpotRent.Services.Payment;

namespace SpotRent.Services.Subscriptions;

public class SubscriptionService : BaseService<SubscriptionService>, ISubscriptionService
{
    private readonly IPaymentService _paymentService;

    public SubscriptionService(
        SpotRentDbContext context,
        ILogger<SubscriptionService> logger,
        IPaymentService paymentService)
        : base(context, logger)
    {
        _paymentService = paymentService;
    }

    public async Task<Result<SubscriptionCreationResponse>> SubscribeAsync(int userId, int subscriptionPlanId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        SubscriptionPlan subscriptionPlan;
        try
        {
            var user = await Context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                return Result.Fail<SubscriptionCreationResponse>($"No user {userId} specified in order request");
            }

            var existingSubscription =
                await Context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            if (existingSubscription is not null)
            {
                return Result.Fail<SubscriptionCreationResponse>(
                    $"User {userId} is already subscribed. Update subscription instead");
            }

            subscriptionPlan =
                await Context.SubscriptionPlans.FindAsync(new object[] { subscriptionPlanId }, cancellationToken);
            if (subscriptionPlan is null || !subscriptionPlan.IsActive)
            {
                return Result.Fail<SubscriptionCreationResponse>(
                    $"Subscription plan with id {subscriptionPlanId} not available");
            }

            var subscription = await CreateSubscription();
            if (subscription is null)
            {
                return Result.Fail<SubscriptionCreationResponse>("Invalid duration");
            }

            var paymentDataResult =
                await _paymentService.CreatePaymentAsync(subscription.Id, subscription.TotalAmount, cancellationToken);
            if (paymentDataResult.Failure)
            {
                return Result.Fail<SubscriptionCreationResponse>($"{paymentDataResult.Error}");
            }

            scope.Complete();

            return Result.Success(new SubscriptionCreationResponse
                { SubscriptionId = subscription.Id, LiqPayPaymentData = paymentDataResult.Value });
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.Subscribe,
                "DB error subscribing user {userId} to plan {planId}. Error: {error}",
                userId, subscriptionPlanId, e.Message);

            return Result.Fail<SubscriptionCreationResponse>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.Subscribe,
                "Error subscribing user {userId} to plan {planId}. Error: {error}",
                userId, subscriptionPlanId, e.Message);

            return Result.Fail<SubscriptionCreationResponse>($"Failure placing order: {e.Message}");
        }

        async Task<Subscription> CreateSubscription()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;
            var start = now;
            var end = CalcEndDate(start, subscriptionPlan.Duration);
            if (end is null)
            {
                return null;
            }

            var subscription = new Subscription
            {
                UserId = userId,
                SubscriptionPlanId = subscriptionPlan.Id,
                Price = subscriptionPlan.Price,
                StartDate = start,
                EndDate = end.Value,
                Status = SubscriptionStatus.NotPaid,
                HoursUsed = 0,
                TotalAmount = subscriptionPlan.Price,
                PaymentStatus = PaymentStatus.NotPaid,
                TransactionId = 0, //todo: make nullable
                CreatedAt = now,
                UpdatedAt = now,
                CancelledAt = null
            };

            await Context.AddAsync(subscription, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            return subscription;
        }
    }

    public async Task<Result<IEnumerable<SubscriptionDto>>> GetCurrentUserSubscriptionAsync(int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var user = await Context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user is null)
            {
                return Result.Fail<IEnumerable<SubscriptionDto>>($"No user with id: {userId}");
            }

            var subscriptionsQuery = Context.Subscriptions.Where(IsActive(userId, DateTime.UtcNow));
            var subscriptions = await subscriptionsQuery.ToListAsync(cancellationToken);

            var subDtos = new List<SubscriptionDto>();
            foreach (var subscription in subscriptions)
            {
                var dto = new SubscriptionDto
                {
                    Id = subscription.Id,
                    SubscriptionPlanId = subscription.SubscriptionPlanId,
                    Price = subscription.Price,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    Status = subscription.Status,
                    HoursUsed = subscription.HoursUsed,
                    TotalAmount = subscription.TotalAmount,
                    PaymentStatus = subscription.PaymentStatus,
                    PaymentProcessedAt = subscription.PaymentProcessedAt,
                    PaymentFailureReason = subscription.PaymentFailureReason,
                    CreatedAt = subscription.CreatedAt,
                    UpdatedAt = subscription.UpdatedAt,
                };
                subDtos.Add(dto);
            }

            return Result.Success(subDtos.AsEnumerable());
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetCurrentUserSubscription,
                "DB error retrieving current subscription for user {userId}. Error: {error}",
                userId, e.Message);

            return Result.Fail<IEnumerable<SubscriptionDto>>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetCurrentUserSubscription,
                "Error retrieving current subscription for user {userId}. Error: {error}",
                userId, e.Message);

            return Result.Fail<IEnumerable<SubscriptionDto>>(
                $"Failure retrieving user subscription: {e.Message}.");
        }
    }

    public async Task<Result<IEnumerable<SubscriptionInfo>>> GetSubscriptionHistoryAsync(int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var history = await Context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId)
                .Select(s => new SubscriptionInfo
                {
                    Id = s.Id,
                    SubscriptionPlanId = s.SubscriptionPlanId,
                    SubscriptionPlanName = s.SubscriptionPlan.Name,
                    SubscriptionStatus = s.Status,
                    IsActive = s.IsActive(),
                    StartedAt = s.StartDate,
                    ExpiresAt = s.EndDate,
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<SubscriptionInfo>>(history);
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetSubscriptionHistory,
                "DB error retrieving subscription history for user {userId}. Error: {error}",
                userId, e.Message);

            return Result.Fail<IEnumerable<SubscriptionInfo>>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetSubscriptionHistory,
                "Error retrieving subscription history for user {userId}. Error: {error}",
                userId, e.Message);

            return Result.Fail<IEnumerable<SubscriptionInfo>>(
                $"Failure retrieving subscription plans: {e.Message}.");
        }
    }

    public async Task<Result<Subscription>> GetSubscriptionByIdAsync(int id, int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var subscription = await Context.Subscriptions.Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            var userValid = (await Context.Users.FirstOrDefaultAsync(u =>
                                subscription.UserId == userId || subscription.SubscriptionPlan.OwnerId == userId,
                            cancellationToken)) !=
                            null;

            if (!userValid)
            {
                return Result.Fail<Subscription>("User does not have access to this subscription");
            }

            return subscription is null
                ? Result.Fail<Subscription>($"No subscription plan with id: {id}")
                : Result.Success(subscription);
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetSubscriptionById,
                "DB error retrieving subscription with id {id}. Error: {error}",
                id, e.Message);

            return Result.Fail<Subscription>($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.GetSubscriptionById,
                "Error retrieving subscription with id {id}. Error: {error}",
                id, e.Message);

            return Result.Fail<Subscription>($"Failure retrieving subscription with id {id}: {e.Message}.");
        }
    }

    public async Task<Result> ChangeSubscriptionAsync(int currSubscriptionId, int newPlanId, int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var user = await Context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user is null)
            {
                return Result.Fail($"No user with id {userId}");
            }

            var subscription =
                await Context.Subscriptions.FindAsync(new object[] { currSubscriptionId }, cancellationToken);
            if (subscription is null || subscription.UserId != userId)
            {
                return Result.Fail($"No subscription with id {currSubscriptionId}");
            }

            subscription.SubscriptionPlanId = newPlanId;
            Context.Update(subscription);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (NpgsqlException e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.ChangeSubscription,
                "DB error changing subscription {subscriptionId} to plan {newPlanId}. Error: {error}",
                currSubscriptionId, newPlanId, e.Message);

            return Result.Fail($"DB error: {e.Message}.");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SubscriptionServiceEventIds.ChangeSubscription,
                "Error changing subscription {subscriptionId} to plan {newPlanId}. Error: {error}",
                currSubscriptionId, newPlanId, e.Message);

            return Result.Fail($"Failure changing subscription plan: {e.Message}");
        }
    }

    public async Task<Result> CancelSubscriptionAsync(int subscriptionId, int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var user = await Context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user is null)
            {
                return Result.Fail($"No user with id {userId}");
            }

            var subscription =
                await Context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
            if (subscription is null || subscription.UserId != userId)
            {
                return Result.Fail<LiqPayRefundResponse>(
                    $"No subscription with id {subscriptionId} of the user {userId}");
            }

            var stateTransitionNotValid = subscription.Status ==
                                          (SubscriptionStatus.Cancelled | SubscriptionStatus.Expired);
            if (stateTransitionNotValid)
            {
                return Result.Fail<LiqPayRefundResponse>($"Subscription with id {subscriptionId} can't be cancelled");
            }

            var result = await _paymentService.RefundPaymentAsync(subscriptionId, cancellationToken);
            result.OnSuccess(() => Serilog.Log.Information("Success refunding payment"))
                .OnFailure(() => Serilog.Log.Error(result.Error));

            if (result.Value.Result == "error")
            {
                return Result.Fail<LiqPayRefundResponse>($"Error refunding: {result.Value.Status}");
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            Context.Subscriptions.Update(subscription);
            await Context.SaveChangesAsync(cancellationToken);

            return result.Failure
                ? Result.Fail<LiqPayRefundResponse>($"Payment refund failed. Reason: {result.Error}")
                : Result.Success(result.Value);
        }
        catch (Exception e)
        {
            return Result.Fail<LiqPayRefundResponse>($"Error refunding: {e.Message}");
        }
    }

    private static Expression<Func<Subscription, bool>> IsActive(int userId, DateTime now)
    {
        return s =>
            s.UserId == userId &&
            (s.EndDate == null || now < s.EndDate) &&
            s.CancelledAt == null &&
            (
                s.PaymentProcessedAt != null ||
                s.PaymentStatus == PaymentStatus.Paid ||
                s.PaymentStatus == PaymentStatus.TestPaid
            ) &&
            s.HoursUsed <
            (s.SubscriptionPlan != null
                ? s.SubscriptionPlan.IncludedHours
                : int.MaxValue);
    }

    private DateTime? CalcEndDate(DateTime startDate, Duration duration)
    {
        double durationInDays;
        switch (duration)
        {
            case Duration.Week:
                durationInDays = 7;
                break;
            case Duration.TwoWeeks:
                durationInDays = 14;
                break;
            case Duration.Month:
                durationInDays = 30;
                break;
            case Duration.ThreeMonths:
                durationInDays = 90;
                break;
            default:
                return null;
        }

        return startDate.AddDays(durationInDays);
    }
}
