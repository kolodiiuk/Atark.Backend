using System.Text.Json.Serialization;
using SpotRent.Api.Json;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;

namespace SpotRent.Api.Dtos.Spaces;

public record CreateSpaceDto
{
    public string Name { get; init; }

    public string Description { get; init; }

    [JsonConverter(typeof(SpaceTypeJsonConverter))]
    public SpaceType SpaceType { get; init; }

    public string Room { get; set; }

    public int Capacity { get; init; }

    public double AreaSqm { get; init; }

    public decimal HourlyRate { get; init; }

    public int AddressId { get; init; }

    public CreateAddressDto Address { get; init; }

    public string ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; init; }

    public IEnumerable<WorkingHoursDto> WorkingHours { get; set; }

    public IEnumerable<CreateAttributeValueDto> AttributeValues { get; set; }

    public Space MapToSpace()
    {
        return new Space
        {
            Name = Name,
            Description = Description,
            SpaceType = SpaceType,
            Room = Room,
            Capacity = Capacity,
            AreaSqm = AreaSqm,
            HourlyRate = HourlyRate,
            AddressId = AddressId,
            ImageUrl = ImageUrl,
            IsAvailable = IsAvailable,
            CreatedAt = CreatedAt == default ? DateTime.UtcNow : CreatedAt,
            WorkingHours = WorkingHours?.Select(wh => new WorkingHours
            {
                DayOfWeek = wh.DayOfWeek,
                OpenTime = wh.OpenTime,
                CloseTime = wh.CloseTime,
                IsClosed = wh.IsClosed
            }).ToList() ?? new List<WorkingHours>(),
            AttributeValues = BuildAttributeValues()
        };
    }

    private ICollection<AttributeValue> BuildAttributeValues()
    {
        var values = AttributeValues?.Select(av => new AttributeValue
        {
            Id = 0,
            SpaceId = 0,
            AttributeId = av.AttributeId,
            Value = av.Value,
            MinValue = av.MinValue,
            MaxValue = av.MaxValue
        }).ToList() ?? new List<AttributeValue>();

        return values;
    }
}
