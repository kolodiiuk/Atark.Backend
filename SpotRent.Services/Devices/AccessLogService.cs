using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Logging;

namespace SpotRent.Services.Devices;

public class AccessLogService : BaseService<AccessLogService>, IAccessLogService
{
    public AccessLogService(SpotRentDbContext context, ILogger<AccessLogService> logger) : base(context, logger)
    {
    }

    public async Task<Result<int>> LogAccessAsync(
        int userId,
        int deviceId,
        AccessType accessType,
        int bookingId,
        CancellationToken cancellationToken,
        bool isSuccessful = true,
        string errorMessage = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var userExists = await Context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return Result.Fail<int>($"User with id {userId} does not exist");
            }

            var deviceExists = await Context.Devices.AnyAsync(d => d.Id == deviceId, cancellationToken);
            if (!deviceExists)
            {
                return Result.Fail<int>($"Device with id {deviceId} does not exist");
            }

            var booking = await Context.Bookings.FindAsync(new object[] { bookingId }, cancellationToken);
            if (booking is null)
            {
                return Result.Fail<int>($"Booking with id {bookingId} does not exist");
            }

            var log = new AccessLog
            {
                UserId = userId,
                DeviceId = deviceId,
                SpaceId = booking.SpaceId,
                AccessType = accessType,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage
            };

            await Context.AddAsync(log, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success(log.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<int>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorCreatingAccessLogEntry,
                "Error creating access log. Error: {e.Message}", e.Message);

            return Result.Fail<int>(
                $"Error creating access log. Error: {e.Message}");
        }
    }

    public async Task<Result<int>> LogOwnerAccessAsync(
        int userId,
        int deviceId,
        AccessType accessType,
        CancellationToken cancellationToken,
        bool isSuccessful = true,
        string errorMessage = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var userExists = await Context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return Result.Fail<int>($"User with id {userId} does not exist");
            }

            var deviceExists = await Context.Devices.AnyAsync(d => d.Id == deviceId, cancellationToken);
            if (!deviceExists)
            {
                return Result.Fail<int>($"Device with id {deviceId} does not exist");
            }

            var space = await Context.Spaces.FirstOrDefaultAsync(s => s.OwnerId == userId, cancellationToken);
            if (space is null)
            {
                return Result.Fail<int>("Space is not found");
            }

            var log = new AccessLog
            {
                UserId = userId,
                DeviceId = deviceId,
                SpaceId = space.Id,
                AccessType = accessType,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage
            };

            await Context.AddAsync(log, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            return Result.Success(log.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<int>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorCreatingAccessLogEntryOwner,
                "Error creating access log. Error: {e.Message}", e.Message);

            return Result.Fail<int>(
                $"Error creating access log. Error: {e.Message}");
        }
    }

    public async Task<Result<IEnumerable<AccessLog>>> GetSpaceAccessLogsAsync(int deviceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessLogs = await Context.AccessLogs
                .Where(al => al.DeviceId == deviceId)
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<AccessLog>>(accessLogs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<IEnumerable<AccessLog>>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorAccessLogsRetrieval,
                "Error getting access logs for space with device {deviceId}. Error: {e.Message}",
                deviceId, e.Message);

            return Result.Fail<IEnumerable<AccessLog>>(
                $"Error getting access logs for space with device {deviceId}. Error: {e.Message}");
        }
    }

    public async Task<Result<IEnumerable<AccessLog>>> GetOwnerAccessLogsAsync(int ownerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessLogs = await Context.AccessLogs
                .Include(al => al.Space)
                .Where(al => al.Space.OwnerId == ownerId)
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<AccessLog>>(accessLogs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<IEnumerable<AccessLog>>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorAccessLogsRetrieval,
                "Error getting access logs for owner {ownerId}. Error: {e.Message}",
                ownerId, e.Message);

            return Result.Fail<IEnumerable<AccessLog>>(
                $"Error getting access logs for owner {ownerId}. Error: {e.Message}");
        }
    }

    public async Task<Result<IEnumerable<AccessLog>>> GetUserAccessLogsAsync(int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessLogs = await Context.AccessLogs
                .Where(al => al.UserId == userId)
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<AccessLog>>(accessLogs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<IEnumerable<AccessLog>>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorAccessLogsRetrieval,
                "Error getting access logs of user {userId}. Error: {e.Message}",
                userId, e.Message);

            return Result.Fail<IEnumerable<AccessLog>>(
                $"Error getting access logs of user {userId}. Error: {e.Message}");
        }
    }

    public async Task<Result<AccessLog>> GetLogById(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var accessLog = await Context.AccessLogs.FindAsync(new object[] { id }, cancellationToken);

            if (accessLog is null)
            {
                return Result.Fail<AccessLog>($"Log with id {id} is not found");
            }

            return Result.Success(accessLog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<AccessLog>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, AccessLogServiceEventIds.ErrorAccessLogRetrieval,
                "Error getting access log {id}. Error: {e.Message}",
                id, e.Message);

            return Result.Fail<AccessLog>($"Error getting access log {id}. Error: {e.Message}");
        }
    }
}
