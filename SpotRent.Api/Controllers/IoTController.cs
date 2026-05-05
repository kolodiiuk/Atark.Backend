using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SpotRent.Api.Dtos.Iot;
using SpotRent.Api.Logging;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Interfaces;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IoTController : BaseController<IoTController>
{
    private readonly ISmartLockService _smartLockService;

    private readonly IQrScannerService _qrScannerService;

    public IoTController(
        ISmartLockService smartLockService,
        IQrScannerService qrScannerService,
        ILogger<IoTController> logger)
        : base(logger)
    {
        _smartLockService = smartLockService;
        _qrScannerService = qrScannerService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Register a new device")]
    [EndpointDescription("Registers a smart lock device for a given space.")]
    public async Task<IActionResult> RegisterDevice(DeviceRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.RegisterDeviceAttempt,
            "Device registration attempt for space {SpaceId}", request?.SpaceId);

        if (request is null || request.SpaceId < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.RegisterDeviceInvalid,
                "Device registration payload is invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Space id must be provided");
        }

        var result = await _smartLockService.RegisterDeviceAsync(request.SpaceId, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.RegisterDeviceSuccess,
                    "Device registered for space {SpaceId}", request.SpaceId))
            .OnFailure(() =>
                Log(LogLevel.Error, IotControllerEventIds.RegisterDeviceFailure,
                    "Device registration failed for space {SpaceId}. Error: {Error}",
                    request.SpaceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("device-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Update device status")]
    [EndpointDescription("Updates the online/offline state and status message reported by a device.")]
    public async Task<IActionResult> UpdateDeviceStatus(DeviceStatusRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.DeviceStatusUpdateAttempt,
            "Device status update attempt for device {DeviceId}", request?.DeviceId);

        if (request is null || request.DeviceId < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.DeviceStatusUpdateInvalid,
                "Device status payload is invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Device id must be provided");
        }

        var result = await _smartLockService.UpdateDeviceStatusAsync(
            request.DeviceId,
            request.IsOnline,
            request.StatusMessage,
            cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.DeviceStatusUpdateSuccess,
                    "Device status updated for {DeviceId}", request.DeviceId))
            .OnFailure(() =>
                Log(LogLevel.Error, IotControllerEventIds.DeviceStatusUpdateFailure,
                    "Device status update failed for {DeviceId}. Error: {Error}",
                    request.DeviceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status200OK);
    }

    [HttpPost("device-id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Resolve a device id")]
    [EndpointDescription("Looks up the device identifier using either a space id or a booking id and returns it in a DTO.")]
    public async Task<ActionResult<DeviceIdResponse>> GetDeviceId(DeviceIdLookupRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.GetDeviceIdAttempt,
            "Device id lookup attempt for {LookupType} id {Id}",
            request?.IsSpaceId == true ? "space" : "booking", request?.Id);

        if (request is null || request.Id < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.GetDeviceIdInvalid,
                "Device id lookup payload is invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Id must be provided");
        }

        var result = await _smartLockService.GetDeviceIdAsync(request.Id, request.IsSpaceId, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.GetDeviceIdSuccess,
                    "Resolved device id {DeviceId} for {LookupType} id {Id}",
                    result.Value,
                    request.IsSpaceId ? "space" : "booking",
                    request.Id))
            .OnFailure(() =>
                Log(LogLevel.Warning, IotControllerEventIds.GetDeviceIdFailure,
                    "Failed to resolve device id for {LookupType} id {Id}. Error: {Error}",
                    request.IsSpaceId ? "space" : "booking",
                    request.Id,
                    result.Error));

        if (result.Failure)
        {
            return result.Error.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                   result.Error.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                ? StatusCode(StatusCodes.Status404NotFound, result.Error)
                : StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }

        return StatusCode(StatusCodes.Status200OK, new DeviceIdResponse(result.Value));
    }

    [HttpPost("unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Unlock device request")]
    [EndpointDescription("Validates QR token and unlocks the device for a user or owner override.")]
    public async Task<IActionResult> Unlock([FromBody] UnlockDeviceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.UnlockAttempt,
            "Unlock attempt device {DeviceId} by user {UserId}",
            request?.DeviceId, request?.UserId);

        if (request is null ||
            request.DeviceId < 1 ||
            request.UserId < 1 ||
            string.IsNullOrWhiteSpace(request.QrCode))
        {
            Log(LogLevel.Warning, IotControllerEventIds.UnlockInvalid,
                "Unlock payload invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Invalid unlock payload");
        }

        var result = request.IsOwnerOverride
            ? await _smartLockService.UnlockOwnerAsync(request.UserId, request.DeviceId, request.QrCode,
                cancellationToken)
            : await _smartLockService.UnlockAsync(request.UserId, request.DeviceId, request.QrCode, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.UnlockSuccess,
                    "Unlock successful for device {DeviceId}", request.DeviceId))
            .OnFailure(() =>
                Log(LogLevel.Warning, IotControllerEventIds.UnlockFailure,
                    "Unlock failed for device {DeviceId}. Error: {Error}",
                    request.DeviceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status200OK, new { Unlocked = result.Value });
    }

    [HttpPost("lock/{deviceId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Lock device")]
    [EndpointDescription("Locks the specified device manually.")]
    public async Task<IActionResult> Lock(int deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.LockAttempt,
            "Lock attempt for device {DeviceId}", deviceId);

        if (deviceId < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.LockInvalid,
                "Lock payload invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Device id must be positive");
        }

        var result = await _smartLockService.LockAsync(deviceId, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.LockSuccess,
                    "Device {DeviceId} locked", deviceId))
            .OnFailure(() =>
                Log(LogLevel.Error, IotControllerEventIds.LockFailure,
                    "Failed to lock device {DeviceId}. Error: {Error}",
                    deviceId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status200OK);
    }

    [HttpPost("qr/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Generate QR code for booking")]
    [EndpointDescription("Generates an encrypted QR payload for a device booking.")]
    public async Task<IActionResult> GenerateQrCode([FromBody] GenerateQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.GenerateQrAttempt,
            "Generate QR attempt  booking {BookingId}", request?.BookingId);

        if (request is null || request.BookingId < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.GenerateQrInvalid,
                "Generate QR payload invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Device and booking must be provided");
        }

        var result = await _qrScannerService.GenerateQrCode(request.BookingId, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, IotControllerEventIds.GenerateQrSuccess,
                    "Generated QR for booking {BookingId}", request.BookingId))
            .OnFailure(() =>
                Log(LogLevel.Error, IotControllerEventIds.GenerateQrFailure,
                    "Failed to generate QR for booking {BookingId}. Error: {Error}",
                    request.BookingId, result.Error));

        return result.Failure
            ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
            : StatusCode(StatusCodes.Status200OK, new { QrCode = result.Value });
    }

    [HttpPost("qr/owner/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Generate QR code for owner access")]
    [EndpointDescription("Generates an encrypted QR payload for the owner of space.")]
    public async Task<IActionResult> GenerateQrCodeOwner(int deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, IotControllerEventIds.GenerateQrAttemptOwner,
            "Generate QR attempt device {DeviceId} booking {BookingId}",
            deviceId);

        if (deviceId < 1)
        {
            Log(LogLevel.Warning, IotControllerEventIds.GenerateQrInvalid,
                "Generate QR payload invalid");

            return StatusCode(StatusCodes.Status400BadRequest, "Device and booking must be provided");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Subscription history fetching attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Subscription history fetching failed: user ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _qrScannerService.GenerateQrCodeOwner(deviceId, parsedUserId, cancellationToken);
            result.OnSuccess(() =>
                    Log(LogLevel.Information, IotControllerEventIds.GenerateQrSuccessOwner,
                        "Generated QR for device {DeviceId}", deviceId))
                .OnFailure(() =>
                    Log(LogLevel.Error, IotControllerEventIds.GenerateQrFailureOwner,
                        "Failed to generate QR for device {DeviceId}. Error: {Error}",
                        deviceId, result.Error));

            return result.Failure
                ? StatusCode(StatusCodes.Status400BadRequest, result.Error)
                : StatusCode(StatusCodes.Status200OK, new { QrCode = result.Value });
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }
}
