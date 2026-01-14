using SpotRent.Domain.Common;

namespace SpotRent.Services.Interfaces;

public interface ISmartLockService
{
    Task<Result> RegisterDeviceAsync(int spaceId, CancellationToken cancellationToken);

    Task<Result> UpdateDeviceStatusAsync(int deviceId, bool isOnline, string statusMessage,
        CancellationToken cancellationToken);

    Task<Result<bool>> UnlockAsync(int userId, int deviceId, string qrCode, CancellationToken cancellationToken);

    Task<Result<bool>> UnlockOwnerAsync(int userId, int deviceId, string qrCode, CancellationToken cancellationToken);

    Task<Result> LockAsync(int deviceId, CancellationToken cancellationToken);
}
