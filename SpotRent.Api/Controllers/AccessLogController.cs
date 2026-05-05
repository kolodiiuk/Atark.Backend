using Microsoft.AspNetCore.Mvc;
using SpotRent.Api.Dtos.Iot;
using SpotRent.Api.Logging;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Devices;
using SpotRent.Services.Interfaces;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/access-log")]
public class AccessLogController : BaseController<AccessLogController>
{
    private readonly IAccessLogService _accessLogService;

    public AccessLogController(IAccessLogService accessLogService, ILogger<AccessLogController> logger)
        : base(logger)
    {
        _accessLogService = accessLogService;
    }

    // [Authorize(Roles = "User")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create access log entry")]
    [EndpointDescription(
        "Creates an access log entry for a user device access attempt. Returns created id on success.")]
    public async Task<IActionResult> CreateLogEntry(LogAccessRequest request, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.CreateAccessLogAttempt,
            "Access log creation attempt");

        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || request.UserId < 1 || request.DeviceId < 1 || !request.BookingId.HasValue)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.CreateAccessLogInvalid,
                "Access log payload contains invalid identifiers");

            return StatusCode(StatusCodes.Status400BadRequest, "Request payload is not valid");
        }

        var serviceDto = new LogAccessRequestDto
        {
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            AccessType = request.AccessType,
            BookingId = request.BookingId.Value,
            IsSuccessful = request.IsSuccessful,
            ErrorMessage = request.ErrorMessage
        };

        var result = await _accessLogService.LogAccessAsync(serviceDto, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.CreateAccessLogSuccess,
                    "Access log entry created with id {AccessLogId}", result.Value))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.CreateAccessLogFailure,
                    "Failed to create access log entry for user {UserId}, device {DeviceId}. Error: {Error}",
                    request.UserId, request.DeviceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
            : StatusCode(StatusCodes.Status201Created, new { Id = result.Value });
    }

    // [Authorize("Owner")]
    [HttpPost("owner")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create owner access log entry")]
    [EndpointDescription("Creates an access log entry initiated by an owner. Returns created id on success.")]
    public async Task<IActionResult> CreateOwnerLogEntry(LogAccessRequest request, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.CreateAccessLogAttemptOwner,
            "Access log creation attempt by owner");

        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || request.UserId < 1 || request.DeviceId < 1)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.CreateAccessLogInvalid,
                "Access log payload contains invalid identifiers");

            return StatusCode(StatusCodes.Status400BadRequest, "Request payload is not valid");
        }

        var serviceDto = new LogOwnerAccessRequestDto
        {
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            AccessType = request.AccessType,
            IsSuccessful = request.IsSuccessful,
            ErrorMessage = request.ErrorMessage
        };

        var result = await _accessLogService.LogOwnerAccessAsync(serviceDto, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.CreateAccessLogSuccess,
                    "Access log entry created with id {AccessLogId}", result.Value))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.CreateAccessLogFailure,
                    "Failed to create access log entry for user {UserId}, device {DeviceId}. Error: {Error}",
                    request.UserId, request.DeviceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
            : StatusCode(StatusCodes.Status201Created, new { Id = result.Value });
    }

    // [Authorize(Roles = "Owner")]
    [HttpGet("space/{spaceId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get space access logs")]
    [EndpointDescription("Retrieves access logs for a specific space.")]
    public async Task<IActionResult> GetSpaceLogs(int spaceId, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.GetSpaceLogsAttempt,
            "Get space logs attempt for space {SpaceId}", spaceId);

        cancellationToken.ThrowIfCancellationRequested();

        if (spaceId < 1)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.GetSpaceLogsInvalid,
                "Space identifier is not valid");

            return StatusCode(StatusCodes.Status400BadRequest, "Space id is not valid");
        }

        var result = await _accessLogService.GetSpaceAccessLogsAsync(spaceId, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.GetSpaceLogsSuccess,
                    "Retrieved space access logs for space {SpaceId}", spaceId))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.GetSpaceLogsFailure,
                    "Failed to retrieve space access logs for space {SpaceId}. Error: {Error}",
                    spaceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
            : StatusCode(StatusCodes.Status200OK, result.Value);
    }

    // [Authorize(Roles = "Owner")]
    [HttpGet("owner/{ownerId:int}")]
    public async Task<IActionResult> GetOwnerLogs(int ownerId, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.GetOwnerLogsAttempt,
            "Get owner logs attempt for owner {OwnerId}", ownerId);

        cancellationToken.ThrowIfCancellationRequested();

        if (ownerId < 1)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.GetOwnerLogsInvalid,
                "Owner identifier is not valid");

            return StatusCode(StatusCodes.Status400BadRequest, "Owner id is not valid");
        }

        var result = await _accessLogService.GetOwnerAccessLogsAsync(ownerId, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.GetOwnerLogsSuccess,
                    "Retrieved access logs for owner {OwnerId}", ownerId))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.GetOwnerLogsFailure,
                    "Failed to retrieve owner access logs for owner {OwnerId}. Error: {Error}",
                    ownerId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
            : StatusCode(StatusCodes.Status200OK, result.Value);
    }

    // [Authorize(Roles = "User")]
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get user access logs")]
    [EndpointDescription("Retrieves access logs for a specific user.")]
    public async Task<IActionResult> GetUserLogs(int userId, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.GetUserLogsAttempt,
            "Get user logs attempt for user {UserId}", userId);

        cancellationToken.ThrowIfCancellationRequested();

        if (userId < 1)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.GetUserLogsInvalid,
                "User identifier is not valid");

            return StatusCode(StatusCodes.Status400BadRequest, "User id is not valid");
        }

        var result = await _accessLogService.GetUserAccessLogsAsync(userId, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.GetUserLogsSuccess,
                    "Retrieved access logs for user {UserId}", userId))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.GetUserLogsFailure,
                    "Failed to retrieve access logs for user {UserId}. Error: {Error}",
                    userId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status500InternalServerError, result.Error)
            : StatusCode(StatusCodes.Status200OK, result.Value);
    }

    // [Authorize(Roles = "User")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get access log by id")]
    [EndpointDescription("Retrieves a single access log entry by its identifier. Returns 404 if not found.")]
    public async Task<IActionResult> GetLogById(int id, CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, AccessLogControllerEventIds.GetLogByIdAttempt,
            "Get access log by id attempt for id {AccessLogId}", id);

        cancellationToken.ThrowIfCancellationRequested();

        if (id < 1)
        {
            Log(LogLevel.Warning, AccessLogControllerEventIds.GetLogByIdInvalid,
                "Access log identifier is not valid");

            return StatusCode(StatusCodes.Status400BadRequest, "Id is not valid");
        }

        var result = await _accessLogService.GetLogById(id, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, AccessLogControllerEventIds.GetLogByIdSuccess,
                    "Retrieved access log with id {AccessLogId}", id))
            .OnFailure(() =>
                Log(LogLevel.Error, AccessLogControllerEventIds.GetLogByIdFailure,
                    "Failed to retrieve access log with id {AccessLogId}. Error: {Error}",
                    id, result.Error));

        if (result.Failure)
        {
            var status = result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status500InternalServerError;

            return StatusCode(status, result.Error);
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }
}
