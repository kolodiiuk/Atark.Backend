using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpotRent.Domain.Common;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Logging;

namespace SpotRent.Services.Devices;

public class QrScannerService : BaseService<QrScannerService>, IQrScannerService
{
    private readonly byte[] _key = "A1B2C3D4E5F6G7H8"u8.ToArray();

    private readonly byte[] _iv = "1A2B3C4D5E6F7G8H"u8.ToArray();

    public QrScannerService(SpotRentDbContext context, ILogger<QrScannerService> logger)
        : base(context, logger)
    {
    }

    public async Task<Result<string>> GenerateQrCode(int bookingId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var booking = await Context.Bookings.FindAsync(new object[] { bookingId }, cancellationToken);
            if (booking is null)
            {
                return Result.Fail<string>($"Booking with id {bookingId} does not exist");
            }

            var payload = new
            {
                BookingId = bookingId,
                ExpirationUtc = booking.EndTime
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var encrypted = Encrypt(json);

            return Result.Success(encrypted);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<string>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, QrScannerServiceEventIds.ErrorGeneratingQrCode,
                "Error generating QR code for booking {bookingId}. Error: {e.Message}",
                bookingId, e.Message);

            return Result.Fail<string>(
                $"Error generating QR code for booking {bookingId}. Error: {e.Message}");
        }
    }

    public async Task<Result<string>> GenerateQrCodeOwner(int spaceId, int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var spaceExists = await Context.Spaces.AnyAsync(s => s.Id == spaceId, cancellationToken);
            if (!spaceExists)
            {
                return Result.Fail<string>($"Space with id {spaceId} does not exist");
            }

            var user = await Context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user is null)
            {
                return Result.Fail<string>($"User with id {userId} does not exist");
            }

            var payload = new
            {
                SpaceId = spaceId,
                BookingId = userId,
                ExpirationUtc = DateTime.UtcNow + new TimeSpan(3, 0, 0)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var encrypted = Encrypt(json);

            return Result.Success(encrypted);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<string>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, QrScannerServiceEventIds.ErrorGeneratingQrCode,
                "Error generating QR code for space {spaceId} and owner {userId}. Error: {e.Message}",
                spaceId, userId, e.Message);

            return Result.Fail<string>(
                $"Error generating QR code for space {spaceId} and user {userId}. Error: {e.Message}");
        }
    }

    public async Task<Result<bool>> ValidateQrCode(string qrCode, int bookingId, CancellationToken cancellationToken,
        int? spaceId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return Result.Fail<bool>("QR code is empty");
            }

            var decrypted = Decrypt(qrCode);

            var payload = System.Text.Json.JsonSerializer.Deserialize<QrPayload>(decrypted);
            if (payload is null)
            {
                return Result.Fail<bool>("Invalid QR payload");
            }

            if (spaceId == null)
            {
                var bookingExists = await Context.Bookings.AnyAsync(b => b.Id == bookingId, cancellationToken);
                if (!bookingExists)
                {
                    return Result.Fail<bool>($"Booking with id {bookingId} does not exist");
                }

                if (payload.BookingId == bookingId && DateTime.UtcNow > payload.ExpirationUtc)
                {
                    return Result.Success(true);
                }

                return Result.Fail<bool>("QR code validation failed");
            }
            else
            {
                // if (payload.SpaceId != null && payload.SpaceId == spaceId && DateTime.UtcNow > payload.ExpirationUtc)
                // {
                return Result.Success(true);
                // }

                if (payload.SpaceId != spaceId || payload.BookingId != bookingId)
                {
                    return Result.Fail<bool>("QR data does not match expected space or booking");
                }

                return Result.Success(true);
            }
        }
        catch (CryptographicException e)
        {
            Log(LogLevel.Warning, QrScannerServiceEventIds.InvalidQrCodeDecryption,
                "Decryption failed for QR code. Error: {e.Message}", e.Message);

            return Result.Fail<bool>("Invalid or tampered QR code");
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<bool>("Operation cancelled");
        }
        catch (Exception e)
        {
            Log(LogLevel.Error, QrScannerServiceEventIds.ErrorValidatingQrCode,
                "Error validating QR code for booking {bookingId}. Error: {e.Message}", bookingId, e.Message);

            return Result.Fail<bool>(
                $"Error validating QR code. Error: {e.Message}");
        }
    }

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipher)
    {
        var buffer = Convert.FromBase64String(cipher);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    private class QrPayload
    {
        public int? SpaceId { get; set; }

        public int BookingId { get; set; }

        public DateTime ExpirationUtc { get; set; }
    }
}
