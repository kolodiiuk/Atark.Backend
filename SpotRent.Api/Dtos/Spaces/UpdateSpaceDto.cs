using System.Text.Json.Serialization;
using SpotRent.Api.Json;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;

namespace SpotRent.Api.Dtos.Spaces;

public record UpdateSpaceDto
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

    public int OwnerId { get; set; }

    public IEnumerable<WorkingHoursDto> WorkingHours { get; set; }

    public IEnumerable<UpdateAttributeValueDto> AttributeValues { get; set; }

    public Space MapToSpace(int spaceId)
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
            OwnerId = OwnerId != 0 ? OwnerId : 0,
            WorkingHours = WorkingHours?.Select(wh => new WorkingHours
            {
                Id = wh.Id,
                DayOfWeek = wh.DayOfWeek,
                OpenTime = wh.OpenTime,
                CloseTime = wh.CloseTime,
                IsClosed = wh.IsClosed,
                SpaceId = spaceId
            }).ToList() ?? new List<WorkingHours>(),
            AttributeValues = AttributeValues?.Select(av => new AttributeValue
            {
                Id = av.Id,
                SpaceId = spaceId, // ensure correct FK
                AttributeId = av.AttributeId,
                Value = av.Value,
                MaxValue = av.MaxValue,
                MinValue = av.MinValue
            }).ToList() ?? new List<AttributeValue>()
        };
    }
}
