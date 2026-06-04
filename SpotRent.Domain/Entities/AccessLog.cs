using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SpotRent.Domain.Enums;

namespace SpotRent.Domain.Entities;

public class AccessLog
{
    public int Id { get; set; }
        
    public int? UserId { get; set; }

    public int? SpaceId { get; set; }

    public int? DeviceId { get; set; }

    [Required]
    public AccessType AccessType { get; set; }
        
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
    public bool IsSuccessful { get; set; } = true;
        
    public string ErrorMessage { get; set; }
        
    public User User { get; set; }

    [JsonIgnore]
    public Space Space { get; set; }

    public Device Device { get; set; }
}
