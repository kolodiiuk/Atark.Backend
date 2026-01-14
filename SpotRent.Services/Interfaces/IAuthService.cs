using Google.Apis.Auth;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Auth;

namespace SpotRent.Services.Interfaces;

public interface IAuthService
{
    Task<Result<User>> ValidateUserCredentials(string email, string password, CancellationToken cancellationToken);

    Task<(string token, string refreshToken)> GenerateTokens(User user, CancellationToken cancellationToken);

    Task<Result<GoogleJsonWebSignature.Payload>> ValidateGoogleSignInRequestAsync(string idToken,
        CancellationToken cancellationToken);

    Task<Result> RegisterAsync(User user, string password, string phoneNumber, string firstName, string lastName,
        CancellationToken cancellationToken);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(string token, CancellationToken cancellationToken);

    Task<Result<User>> GetUserAsync(int userId, CancellationToken cancellationToken);

    Task<Result<User>> GetOrCreateUser(string payloadEmail, string payloadName, string payloadSubject,
        CancellationToken cancellationToken);
}
