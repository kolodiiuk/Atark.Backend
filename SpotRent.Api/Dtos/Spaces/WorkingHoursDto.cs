using System.Text.Json.Serialization;
using SpotRent.Api.Json;
using SpotRent.Domain.Entities;

namespace SpotRent.Api.Dtos.Spaces;

public class WorkingHoursDto
{
    public int Id { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    [JsonConverter(typeof(TimeOnlyJsonConverter))]
    public TimeOnly OpenTime { get; set; }

    [JsonConverter(typeof(TimeOnlyJsonConverter))]
    public TimeOnly CloseTime { get; set; }

    public bool IsClosed { get; set; }

    public static WorkingHoursDto Map(WorkingHours wh)
    {
        if (wh is null)
        {
            return null;
        }
        
        return new WorkingHoursDto
        {
            Id = wh.Id,
            DayOfWeek = wh.DayOfWeek,
            OpenTime = wh.OpenTime,
            CloseTime = wh.CloseTime,
            IsClosed = wh.IsClosed
        };
    }
}
