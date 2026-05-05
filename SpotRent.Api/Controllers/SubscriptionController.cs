using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotRent.Api.Logging;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : BaseController<SubscriptionController>
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
        : base(logger)
    {
        _subscriptionService = subscriptionService;
    }

    [Authorize(Roles = "User")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Creates a subscription for a user.")]
    [EndpointDescription("Validates the subscription payload and subscribes the specified user to the requested plan.")]
    public async Task<ActionResult> CreateSubscriptionAsync(CreateSubscriptionDto subscriptionDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!subscriptionDto.IsValid())
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Id of user or plan is not valid");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription creation attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription creation failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _subscriptionService.SubscribeAsync(parsedUserId, subscriptionDto.PlanId, cancellationToken);

            result.OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.Subscribe,
                    "Subscription created for user {userId}, plan {planId}.",
                    parsedUserId, subscriptionDto.PlanId))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.Subscribe,
                    "Error creating subscription for user {userId}. Error: {error}",
                    parsedUserId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "User, Owner")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Subscription>> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (id < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Id is not valid");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _subscriptionService.GetSubscriptionByIdAsync(id, parsedUserId, cancellationToken);

            result.OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.GetSubscriptionById,
                    "Retrieved subscription with id {id}.", id))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.GetSubscriptionById,
                    "Error retrieving subscription with id {id}. Error: {error}", id, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                : StatusCode(StatusCodes.Status200OK, SubscriptionDto.MapSubscription(result.Value));
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "User")]
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSubscriptionHistoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _subscriptionService.GetSubscriptionHistoryAsync(parsedUserId, cancellationToken);

            result.OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.GetSubscriptionHistory,
                    "Retrieved subscription history for user {userId}.", parsedUserId))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.GetSubscriptionHistory,
                    "Error retrieving subscription history for user {userId}. Error: {error}", parsedUserId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                : StatusCode(StatusCodes.Status200OK, result.Value);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "User")]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetMySubscriptionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            if (parsedUserId < 1)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Id is not valid");
            }

            var result = await _subscriptionService.GetCurrentUserSubscriptionAsync(parsedUserId, cancellationToken);

            result
                .OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.GetMySubscription,
                    "Retrieved current subscription for user {userId}.", parsedUserId))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.GetMySubscription,
                    "Error retrieving current subscription for user {userId}. Error: {error}", parsedUserId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
                : StatusCode(StatusCodes.Status200OK, result.Value);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "User")]
    [HttpPatch("{subscriptionId:int}/change")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangeSubscriptionAsync(int subscriptionId, [FromBody] int newPlanId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (subscriptionId < 1 || newPlanId < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "At least one of ids is not valid");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _subscriptionService.ChangeSubscriptionAsync(subscriptionId, newPlanId, parsedUserId, cancellationToken);

            result.OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.ChangeSubscription,
                    "Changed subscription {subscriptionId} to plan {newPlanId}.", subscriptionId,
                    newPlanId))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.ChangeSubscription,
                    "Error changing subscription {subscriptionId} to plan {newPlanId}. Error: {error}",
                    subscriptionId, newPlanId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status204NoContent);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "User")]
    [HttpPut("{userId:int}/{subscriptionId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CancelSubscriptionAsync(int subscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (subscriptionId < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Subscription id is not valid");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId, parsedUserId, cancellationToken);

            result.OnSuccess(() => Log(LogLevel.Information, SubscriptionControllerEventIds.CancelSubscription,
                    "Cancelled subscription {subscriptionId} for user {userId}.",
                    subscriptionId, parsedUserId))
                .OnFailure(() => Log(LogLevel.Error, SubscriptionControllerEventIds.CancelSubscription,
                    "Error cancelling subscription {subscriptionId} for user {userId}. Error: {error}",
                    subscriptionId, parsedUserId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status204NoContent);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }
}
