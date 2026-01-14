using SpotRent.Domain.Common;

namespace SpotRent.Services.Interfaces;

public interface IQrScannerService
{
    Task<Result<string>> GenerateQrCode(int bookingId, CancellationToken cancellationToken);

    Task<Result<bool>> ValidateQrCode(string qrCode, int bookingId, CancellationToken cancellationToken, int? spaceId);

    Task<Result<string>> GenerateQrCodeOwner(int deviceId, int userId, CancellationToken cancellationToken);
}
