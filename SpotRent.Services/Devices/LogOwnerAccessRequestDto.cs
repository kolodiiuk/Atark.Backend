using SpotRent.Domain.Enums;

namespace SpotRent.Services.Devices;

public class LogOwnerAccessRequestDto
{
    public int UserId { get; set; }

    public int DeviceId { get; set; }

    public AccessType AccessType { get; set; }

    public bool IsSuccessful { get; set; } = true;

    public string ErrorMessage { get; set; }
}
