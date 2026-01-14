using SpotRent.Domain.Enums;

namespace SpotRent.Services.Devices;

public class LogAccessRequestDto
{
    public int UserId { get; set; }

    public int DeviceId { get; set; }

    public AccessType AccessType { get; set; }

    public int BookingId { get; set; }

    public bool IsSuccessful { get; set; } = true;

    public string ErrorMessage { get; set; }
}