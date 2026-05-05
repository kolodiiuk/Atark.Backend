using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Logging;

namespace SpotRent.Services.Devices;

public class SmartLockService : BaseService<SmartLockService>, ISmartLockService
{
    private readonly IQrScannerService _qrScannerService;

    public SmartLockService(
        IQrScannerService qrScannerService,
        SpotRentDbContext context,
        ILogger<SmartLockService> logger)
        : base(context, logger)
    {
        _qrScannerService = qrScannerService;
    }

    public async Task<Result> RegisterDeviceAsync(int spaceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var space = await Context.Spaces.FindAsync(new object[] { spaceId }, cancellationToken);
            if (space is null)
            {
                return Result.Fail($"Space with id {spaceId} doesn't exist");
            }

            var now = DateTime.UtcNow;
            var device = new Device
            {
                SpaceId = spaceId,
                DeviceName = Guid.NewGuid().ToString(),
                Status = LockStatus.Locked,
                IsOnline = true,
                InstalledAt = now,
                UpdatedAt = now
            };

            await Context.Devices.AddAsync(device, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            Log(LogLevel.Information, SmartLockServiceEventIds.RegisterDevice,
                "Registered new device {DeviceId} for space {SpaceId}", device.Id, spaceId);

            return Result.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SmartLockServiceEventIds.Error,
                "Error registering device for space {SpaceId}. Error: {error}", spaceId, e.Message);

            return Result.Fail($"Error registering device: {e.Message}");
        }
    }

    public async Task<Result> UpdateDeviceStatusAsync(int deviceId, bool isOnline, string statusMessage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var device = await Context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
            if (device is null)
            {
                return Result.Fail($"Device with id {deviceId} does not exist");
            }

            device.IsOnline = isOnline;
            device.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(statusMessage) &&
                Enum.TryParse<LockStatus>(statusMessage, true, out var parsedStatus))
            {
                device.Status = parsedStatus;
            }

            Context.Devices.Update(device);
            await Context.SaveChangesAsync(cancellationToken);

            Log(LogLevel.Information, SmartLockServiceEventIds.UpdateDeviceStatus,
                "Updated device {DeviceId} status. IsOnline: {IsOnline}, Status: {Status}",
                deviceId, isOnline, device.Status);

            return Result.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SmartLockServiceEventIds.Error,
                "Error updating device {DeviceId} status. Error: {error}", deviceId, e.Message);

            return Result.Fail($"Error updating device status: {e.Message}");
        }
    }

    public async Task<Result<bool>> UnlockAsync(int userId, int deviceId, string qrCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var userExists = await Context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return Result.Fail<bool>($"User with id {userId} does not exist");
            }

            var device = await Context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
            if (device is null)
            {
                return Result.Fail<bool>($"Device with id {deviceId} does not exist");
            }

            var now = DateTime.UtcNow;
            var booking = await Context.Bookings.FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.SpaceId == device.SpaceId &&
                // b.StartTime <= now &&
                // b.EndTime >= now &&
                b.CancelledAt == null, cancellationToken);

            if (booking is null)
            {
                return Result.Fail<bool>("No active booking found for user on this device's space");
            }

            Log(LogLevel.Information, SmartLockServiceEventIds.UnlockAttempt,
                "Unlock attempt by user {UserId} on device {DeviceId} for booking {BookingId}",
                userId, deviceId, booking.Id);

            var validation = await _qrScannerService.ValidateQrCode(qrCode, deviceId, cancellationToken, booking.Id);
            if (validation.Failure)
            {
                Log(LogLevel.Warning, SmartLockServiceEventIds.UnlockFailure,
                    "Unlock failed for user {UserId} on device {DeviceId}. Reason: {Reason}",
                    userId, deviceId, validation.Error);

                return Result.Fail<bool>(validation.Error);
            }

            return Result.Success(true);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SmartLockServiceEventIds.Error,
                "Error during unlock for user {UserId} device {DeviceId}. Error: {error}",
                userId, deviceId, e.Message);

            return Result.Fail<bool>($"Error unlocking: {e.Message}");
        }
    }

    public async Task<Result<bool>> UnlockOwnerAsync(int userId, int deviceId, string qrCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var userExists = await Context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return Result.Fail<bool>($"User with id {userId} does not exist");
            }

            var device = await Context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
            if (device is null)
            {
                return Result.Fail<bool>($"Device with id {deviceId} does not exist");
            }

            var now = DateTime.UtcNow;
            var isOwner = await Context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId && d.Space.OwnerId == userId,
                cancellationToken);

            if (isOwner is null)
            {
                return Result.Fail<bool>("No active booking found for user on this device's space");
            }

            Log(LogLevel.Information, SmartLockServiceEventIds.UnlockAttempt,
                "Unlock attempt by user {UserId} on device {DeviceId} for booking {BookingId}",
                userId, deviceId, isOwner.Id);

            var validation = await _qrScannerService.ValidateQrCode(qrCode, deviceId, cancellationToken, isOwner.Id);
            if (validation.Failure)
            {
                Log(LogLevel.Warning, SmartLockServiceEventIds.UnlockFailure,
                    "Unlock failed for user {UserId} on device {DeviceId}. Reason: {Reason}",
                    userId, deviceId, validation.Error);

                return Result.Fail<bool>(validation.Error);
            }

            return Result.Success(true);
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SmartLockServiceEventIds.Error,
                "Error during unlock for user {UserId} device {DeviceId}. Error: {error}",
                userId, deviceId, e.Message);

            return Result.Fail<bool>($"Error unlocking: {e.Message}");
        }
    }

    public async Task<Result> LockAsync(int deviceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var device = await Context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
            if (device is null)
            {
                return Result.Fail($"Device with id {deviceId} does not exist");
            }

            device.Status = LockStatus.Locked;
            device.UpdatedAt = DateTime.UtcNow;

            Context.Devices.Update(device);
            await Context.SaveChangesAsync(cancellationToken);

            Log(LogLevel.Information, SmartLockServiceEventIds.LockDevice,
                "Device {DeviceId} locked", deviceId);

            return Result.Success();
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, SmartLockServiceEventIds.Error,
                "Error locking device {DeviceId}. Error: {error}", deviceId, e.Message);

            return Result.Fail($"Error locking device: {e.Message}");
        }
    }

    public async Task<Result<int>> GetDeviceIdAsync(int id, bool isSpaceId, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            int? deviceId = null;
            if (isSpaceId)
            {
                deviceId = await GetDeviceIdBySpaceAsync(id, cancellationToken);

                return Result.Success(deviceId.Value);
            }
            else
            {
                deviceId = await GetDeviceIdByBookingAsync(id, cancellationToken);

                return Result.Success(deviceId.Value);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException e)
        {
            return Result.Fail<int>($"DB problems:{e.Message}");
        }
        catch (Exception e)
        {
            return Result.Fail<int>($"Error getting device: {e.Message}");
        }
    }

    private async Task<int?> GetDeviceIdByBookingAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviceId = await
            (from booking in Context.Bookings
            join space in Context.Spaces on booking.SpaceId equals space.Id
            join device in Context.Devices on space.Id equals device.SpaceId
            where booking.Id == id
            select device.Id).FirstOrDefaultAsync(cancellationToken);

        return deviceId;
    }

    private async Task<int> GetDeviceIdBySpaceAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviceId = await Context.Devices
            .Where(d => d.SpaceId == id)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return deviceId;
    }
}
