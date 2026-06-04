using SpotRent.Domain.Entities;

namespace SpotRent.Api.Dtos.Spaces;

public class CreateAddressDto
{
    public string Building { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string Region { get; set; }

    public Address MapToAddress()
    {
        return new Address
        {
            Building = Building?.Trim(),
            Street = Street?.Trim(),
            City = City?.Trim(),
            Region = Region?.Trim()
        };
    }
}
