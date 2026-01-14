using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;

namespace SpotRent.Services.Interfaces;

public interface IAccessLogService
{
    Task<Result<int>> LogAccessAsync(
        int userId,
        int deviceId,
        AccessType accessType,
        int bookingId,
        CancellationToken cancellationToken,
        bool isSuccessful = true,
        string errorMessage = null);

    Task<Result<int>> LogOwnerAccessAsync(
        int userId,
        int deviceId,
        AccessType accessType,
        CancellationToken cancellationToken,
        bool isSuccessful = true,
        string errorMessage = null);

    Task<Result<IEnumerable<AccessLog>>> GetSpaceAccessLogsAsync(int deviceId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<AccessLog>>> GetOwnerAccessLogsAsync(int ownerId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<AccessLog>>> GetUserAccessLogsAsync(int userId, CancellationToken cancellationToken);

    Task<Result<AccessLog>> GetLogById(int id, CancellationToken cancellationToken);
}
