using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;

namespace SpotRent.Api.Dtos.Auth;

public class UserDto
{
    public int Id { get; set; }

    public string Email { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Role Role { get; set; }

    public string PhoneNumber { get; set; }

    public static UserDto MapUser(User u)
    {
        return new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role,
            PhoneNumber = u.PhoneNumber
        };
    }
}
