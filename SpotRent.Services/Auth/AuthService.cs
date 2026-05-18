using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Infrastructure;
using SpotRent.Services.Interfaces;

namespace SpotRent.Services.Auth;

public class AuthService : IAuthService
{
    private readonly SpotRentDbContext _context;

    private readonly IJwtService _jwtService;

    private readonly UserManager<User> _userManager;

    private readonly IConfiguration _configuration;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(SpotRentDbContext context,
        UserManager<User> userManager,
        IJwtService jwtService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _jwtService = jwtService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<Result> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        request.User.PhoneNumber = request.PhoneNumber ?? "";
        request.User.UserName = request.User.Email;
        request.User.FirstName = request.FirstName;
        request.User.LastName = request.LastName;

        try
        {
            var result = await _userManager.CreateAsync(request.User, request.Password);
            if (!result.Succeeded)
            {
                return Result.Fail($"Failed to create a user: {string.Join(", ",
                    result.Errors.Select(e => e.Description))}");
            }

            var roleResult = await _userManager.AddToRoleAsync(request.User, request.User.Role.ToString());
            if (!roleResult.Succeeded)
            {
                return Result.Fail(
                    $"Failed to assign role: {string.Join(", ",
                        roleResult.Errors.Select(e => e.Description))}");
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail("Operation cancelled");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Result<User>> ValidateUserCredentials(string email, string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !(await VerifyPasswordAsync(user, password, cancellationToken)))
        {
            return Result.Fail<User>("Invalid email or password");
        }

        return Result.Success(user);
    }

    public async Task<(string token, string refreshToken)> GenerateTokens(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var userRefreshToken = new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = GetIpAddress()
        };

        _context.UserRefreshTokens.Add(userRefreshToken);

        var oldTokens = _context.UserRefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.Expires < DateTime.UtcNow);
        _context.UserRefreshTokens.RemoveRange(oldTokens);

        await _context.SaveChangesAsync(cancellationToken);

        return new ValueTuple<string, string>(token, refreshToken);
    }

    public async Task<Result<GoogleJsonWebSignature.Payload>> ValidateGoogleSignInRequestAsync(string idToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<GoogleJsonWebSignature.Payload>("Operation cancelled");
        }
        catch (Exception ex)
        {
            return Result.Fail<GoogleJsonWebSignature.Payload>("Validation error");
        }

        if (payload == null)
        {
            return Result.Fail<GoogleJsonWebSignature.Payload>("Invalid Google token payload");
        }

        var issuer = payload.Issuer;
        if (issuer != "accounts.google.com" && issuer != "https://accounts.google.com")
        {
            return Result.Fail<GoogleJsonWebSignature.Payload>("Invalid token issuer");
        }

        if (!payload.EmailVerified)
        {
            return Result.Fail<GoogleJsonWebSignature.Payload>("Email not verified by Google");
        }

        return Result.Success(payload);
    }

    public async Task<Result<User>> GetOrCreateUser(string payloadEmail, string payloadName, string payloadSubject,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(payloadSubject) || string.IsNullOrEmpty(payloadEmail))
        {
            return Result.Fail<User>("Invalid Google payload");
        }

        var user = await _userManager.FindByLoginAsync("Google", payloadSubject);
        if (user != null)
        {
            return Result.Success(user);
        }

        user = await _userManager.FindByEmailAsync(payloadEmail);
        if (user != null)
        {
            var fixResult = await FixLoginInUserManager(payloadSubject, user, cancellationToken);

            return fixResult.Failure ? Result.Fail<User>(fixResult.Error) : Result.Success(user);
        }

        var userCreateResult = await CreateNewUser(payloadEmail, payloadName, payloadSubject, cancellationToken);
        if (userCreateResult.Failure)
        {
            return Result.Fail<User>(userCreateResult.Error);
        }

        var login = new UserLoginInfo("Google", payloadSubject, "Google");
        var addLogin = await _userManager.AddLoginAsync(userCreateResult.Value, login);
        if (!addLogin.Succeeded)
        {
            return Result.Fail<User>(
                string.Join(", ", addLogin.Errors.Select(e => e.Description)));
        }

        return Result.Success(userCreateResult.Value);
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storedRefreshToken = await _context.UserRefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token && rt.Expires > DateTime.UtcNow, cancellationToken);

        if (storedRefreshToken == null)
        {
            return Result.Fail<RefreshTokenResponse>("Invalid refresh token");
        }

        if (storedRefreshToken.Revoked != null)
        {
            return Result.Fail<RefreshTokenResponse>("Token revoked");
        }

        var newToken = _jwtService.GenerateToken(storedRefreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        storedRefreshToken.Revoked = DateTime.UtcNow;
        storedRefreshToken.RevokedByIp = GetIpAddress();
        storedRefreshToken.ReplacedByToken = newRefreshToken;
        var userRefreshToken = new UserRefreshToken
        {
            UserId = storedRefreshToken.UserId,
            Token = newRefreshToken,
            Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = GetIpAddress()
        };
        _context.UserRefreshTokens.Add(userRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Id = storedRefreshToken.User.Id,
            Email = storedRefreshToken.User.Email,
            FirstName = storedRefreshToken.User.FirstName,
            LastName = storedRefreshToken.User.LastName,
            Role = storedRefreshToken.User.Role,
            PhoneNumber = storedRefreshToken.User.PhoneNumber
        });
    }

    public async Task<Result> LogoutAsync(string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = await _context.UserRefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken != null)
        {
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = GetIpAddress();
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            return Result.Fail("Refresh token wasn't found");
        }

        return Result.Success();
    }

    public async Task<Result<User>> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null)
        {
            return Result.Fail<User>("User is not found");
        }

        return Result.Success(user);
    }

    private async Task<bool> VerifyPasswordAsync(User user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _userManager.CheckPasswordAsync(user, password);
    }

    private string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return null;
        }

        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return context.Request.Headers["X-Forwarded-For"].ToString();
        }

        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0";
    }

    private async Task<Result<User>> CreateNewUser(string payloadEmail, string payloadName, string payloadSubject, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newUser1 = new User
        {
            GoogleId = payloadSubject,
            Email = payloadEmail,
            UserName = payloadEmail,
            FirstName = payloadName,
            LastName = "",
            PhoneNumber = "",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            Role = Role.User
        };

        var createResult = await _userManager.CreateAsync(newUser1);
        if (!createResult.Succeeded)
        {
            return (Result.Fail<User>(
                string.Join(", ", createResult.Errors.Select(e => e.Description))));
        }

        return Result.Success(newUser1);
    }

    private async Task<Result> FixLoginInUserManager(string payloadSubject, User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loginInfo = new UserLoginInfo("Google", payloadSubject, "Google");
        var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
        if (!addLoginResult.Succeeded && addLoginResult.Errors.Any())
        {
            var nonDuplicateErrors = addLoginResult.Errors
                .Where(e =>
                {
                    var desc = e.Description ?? string.Empty;
                    return !(desc.Contains("already", StringComparison.OrdinalIgnoreCase)
                             || desc.Contains("exists", StringComparison.OrdinalIgnoreCase)
                             || desc.Contains("associated", StringComparison.OrdinalIgnoreCase));
                })
                .ToList();

            if (nonDuplicateErrors.Any())
            {
                return Result.Fail(string.Join(", ", nonDuplicateErrors.Select(e => e.Description)));
            }
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        user.GoogleId ??= payloadSubject;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}
