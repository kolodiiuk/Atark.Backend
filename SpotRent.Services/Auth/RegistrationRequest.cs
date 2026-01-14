using SpotRent.Domain.Entities;

namespace SpotRent.Services.Auth;

public class RegistrationRequest
{
    public User User { get; set; }

    public string Password { get; set; }

    public string PhoneNumber { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
}
