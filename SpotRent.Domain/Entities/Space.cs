using SpotRent.Domain.Enums;

namespace SpotRent.Domain.Entities;

public class Space
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public SpaceType SpaceType { get; set; }

    public double AreaSqm { get; set; }

    public string Room { get; set; }

    public int Capacity { get; set; }

    public decimal HourlyRate { get; set; }

    public string ImageUrl { get; set; }

    public int OwnerId { get; set; }

    public int AddressId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Address Address { get; set; }

    public ICollection<AttributeValue> AttributeValues { get; set; } = new List<AttributeValue>();

    public DateTime UpdatedAt { get; set; }

    public User Owner { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
}
