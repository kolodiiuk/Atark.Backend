using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Devices;

namespace SpotRent.Services.Interfaces;

public interface IAccessLogService
{
    Task<Result<int>> LogAccessAsync(LogAccessRequestDto request, CancellationToken cancellationToken);

    Task<Result<int>> LogOwnerAccessAsync(LogOwnerAccessRequestDto request, CancellationToken cancellationToken);

    Task<Result<IEnumerable<AccessLog>>> GetSpaceAccessLogsAsync(int deviceId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<AccessLog>>> GetOwnerAccessLogsAsync(int ownerId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<AccessLog>>> GetUserAccessLogsAsync(int userId, CancellationToken cancellationToken);

    Task<Result<AccessLog>> GetLogById(int id, CancellationToken cancellationToken);
}
