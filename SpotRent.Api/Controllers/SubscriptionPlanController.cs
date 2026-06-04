using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotRent.Api.Logging;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Subscriptions;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/subscription-plans")]
public class SubscriptionPlanController : BaseController<SubscriptionPlanController>
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    public SubscriptionPlanController(
        ISubscriptionPlanService subscriptionPlanService,
        ILogger<SubscriptionPlanController> logger)
        : base(logger)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Creates a new subscription plan.")]
    [EndpointDescription("Validates the incoming plan payload and persists it as a subscription plan definition.")]
    public async Task<ActionResult> CreateSubscriptionPlanAsync([FromBody] CreateSubscriptionPlanDto subscriptionDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId, "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(ownerId, out var parsedOwnerId);
        if (isParsed)
        {
            var result = await _subscriptionPlanService.CreateSubscriptionPlanAsync(parsedOwnerId, subscriptionDto, cancellationToken);
            if (result.Failure)
            {
                return StatusCode(StatusCodes.Status400BadRequest, result.Error);
            }

            return StatusCode(StatusCodes.Status201Created);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Lists all subscription plans.")]
    [EndpointDescription("Retrieves every subscription plan.")]
    public async Task<IActionResult> GetPlansAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _subscriptionPlanService.GetPlansAsync(cancellationToken);
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status400BadRequest, result.Error);
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Lists owner's subscription plans.")]
    [EndpointDescription("Retrieves owner's subscription plan.")]
    public async Task<IActionResult> GetOwnerPlansAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (UserId == null)
        {
            return Problem(title: "No user id specified", statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await _subscriptionPlanService.GetOwnerPlansAsync(UserId ?? 0, cancellationToken);
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status400BadRequest, result.Error);
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a subscription plan by id.")]
    [EndpointDescription("Fetches the plan details for the provided identifier or returns an error if unavailable.")]
    public async Task<IActionResult> GetPlanByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _subscriptionPlanService.GetPlanByIdAsync(id, cancellationToken);
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status400BadRequest, result.Error);
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Updates an existing subscription plan.")]
    [EndpointDescription(
        "Logs the update attempt, validates payload data, and updates the specified subscription plan.")]
    public async Task<ActionResult> UpdateSubscriptionPlanAsync(int id,
        [FromBody] UpdateSubscriptionPlanDto subscriptionPlanDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (id < 1 || subscriptionPlanDto is null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid request data");
        }

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId, "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(ownerId, out var parsedOwnerId);
        if (isParsed)
        {
            Log(LogLevel.Information, SubscriptionPlanControllerEventIds.DeactivateSubscriptionPlanEvent,
                "Updating subscription plan with id {subscriptionPlanId}.", id);

            var result = await _subscriptionPlanService.UpdateSubscriptionPlanAsync(
                id, subscriptionPlanDto, parsedOwnerId, cancellationToken);


            result
                .OnSuccess(() => Log(
                    LogLevel.Information,
                    SubscriptionPlanControllerEventIds.UpdateSubscriptionPlanEvent,
                    "Updated subscription plan with id {subscriptionPlanId}.", id))
                .OnFailure(() => Log(
                    LogLevel.Error,
                    SubscriptionPlanControllerEventIds.UpdateSubscriptionPlanEvent,
                    "Error updating subscription plan with id {subscriptionPlanId}. Error: {error}",
                    id, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status200OK);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "Owner, Admin")]
    [HttpDelete("{subscriptionPlanId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Deletes a subscription plan.")]
    [EndpointDescription("Validates the identifier, logs the outcome, and removes the subscription plan if possible.")]
    public async Task<IActionResult> DeleteSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (subscriptionPlanId < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Id is less than 1");
        }

        Log(
            LogLevel.Information,
            SubscriptionPlanControllerEventIds.DeleteSubscriptionPlanEvent,
            "Deleting subscription plan with id {subscriptionPlanId}.",
            subscriptionPlanId);

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            Log(
                LogLevel.Warning,
                AuthControllerEventIds.TokenVerificationNoUserId,
                "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        if (!int.TryParse(ownerId, out _))
        {
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var result = await _subscriptionPlanService.DeleteSubscriptionPlanAsync(subscriptionPlanId, cancellationToken);

        result
            .OnSuccess(() => Log(
                LogLevel.Information,
                SubscriptionPlanControllerEventIds.DeleteSubscriptionPlanEvent,
                "Deleted subscription plan with id {subscriptionPlanId}.",
                subscriptionPlanId))
            .OnFailure(() => Log(
                LogLevel.Error,
                SubscriptionPlanControllerEventIds.DeleteSubscriptionPlanEvent,
                "Error deleting subscription plan with id {subscriptionPlanId}. Error: {error}",
                subscriptionPlanId,
                result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status200OK);
    }

    [Authorize(Roles = "Owner, Admin")]
    [HttpPut("deactivate/{subscriptionPlanId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Deactivates a subscription plan.")]
    [EndpointDescription("Attempts to deactivate the specified plan while logging success or failure details.")]
    public async Task<IActionResult> DeactivateSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (subscriptionPlanId < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Id is less than 1");
        }

        Log(LogLevel.Information, SubscriptionPlanControllerEventIds.DeactivateSubscriptionPlanEvent,
            "Deactivating subscription plan with id {subscriptionPlanId}.",
            subscriptionPlanId);

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId, "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(ownerId, out _);
        if (isParsed)
        {
            var result = await _subscriptionPlanService.DeactivateSubscriptionPlanAsync(subscriptionPlanId, cancellationToken);

            result.OnSuccess(() => Log(
                    LogLevel.Information, SubscriptionPlanControllerEventIds.DeactivateSubscriptionPlanEvent,
                    "Deactivated subscription plan with id {subscriptionPlanId}.", subscriptionPlanId))
                .OnFailure(() => Log(
                    LogLevel.Error, SubscriptionPlanControllerEventIds.DeactivateSubscriptionPlanEvent,
                    "Error deactivating subscription plan with id {subscriptionPlanId}. Error: {error}",
                    subscriptionPlanId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status200OK);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "Owner, Admin")]
    [HttpPut("activate/{subscriptionPlanId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Activates a subscription plan.")]
    [EndpointDescription("Attempts to activate the specified plan while logging success or failure details.")]
    public async Task<IActionResult> ActivateSubscriptionPlanAsync(int subscriptionPlanId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (subscriptionPlanId < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Id is less than 1");
        }

        Log(LogLevel.Information, SubscriptionPlanControllerEventIds.ActivateSubscriptionPlanEvent,
            "Activating subscription plan with id {subscriptionPlanId}.",
            subscriptionPlanId);

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(ownerId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId, "User ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(ownerId, out _);
        if (isParsed)
        {
            var result = await _subscriptionPlanService.ActivateSubscriptionPlanAsync(subscriptionPlanId, cancellationToken);

            result.OnSuccess(() => Log(
                    LogLevel.Information, SubscriptionPlanControllerEventIds.ActivateSubscriptionPlanEvent,
                    "Activated subscription plan with id {subscriptionPlanId}.", subscriptionPlanId))
                .OnFailure(() => Log(
                    LogLevel.Error, SubscriptionPlanControllerEventIds.ActivateSubscriptionPlanEvent,
                    "Error activating subscription plan with id {subscriptionPlanId}. Error: {error}",
                    subscriptionPlanId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status200OK);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }
}
