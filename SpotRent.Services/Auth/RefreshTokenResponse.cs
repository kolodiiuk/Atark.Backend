using SpotRent.Domain.Enums;

namespace SpotRent.Services.Auth;

public class RefreshTokenResponse
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
    
    public int Id { get; set; }
    
    public string Email { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }

    public Role Role { get; set; }

    public string? PhoneNumber { get; set; }
}
