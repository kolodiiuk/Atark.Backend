using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotRent.Api.Dtos.Auth;
using SpotRent.Api.Logging;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Interfaces;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController<AuthController>
{
    private readonly IAuthService _authService;

    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
        : base(logger)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("google")]
    [EndpointSummary("Signs in a user via Google authentication.")]
    [EndpointDescription(
        "Validates the Google ID token, creates or retrieves the SpotRent user, and issues JWT plus refresh tokens.")]
    public async Task<ActionResult<LoginResponse>> GoogleSignIn([FromBody] GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.GoogleSignInAttempt, "Google sign-in attempt");

        var validationResult = await _authService.ValidateGoogleSignInRequestAsync(request.IdToken, cancellationToken);
        validationResult.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.InvalidGoogleToken, "Invalid Google token"));
        if (validationResult.Failure)
        {
            return Unauthorized(new { message = "Invalid Google token" });
        }

        var payload = validationResult.Value;

        var userResult =
            await _authService.GetOrCreateUser(payload.Email, payload.Name, payload.Subject, cancellationToken);
        userResult.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.GetOrCreateUserFailed,
                "Failed to get or create user for Google email: {Email}. Error: {Error}", payload.Email,
                userResult.Error));
        if (userResult.Failure)
        {
            return Unauthorized(new { message = userResult.Error });
        }

        var tokens = await _authService.GenerateTokens(userResult.Value, cancellationToken);
        var tokenExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"]));
        var response = new LoginResponse
        {
            Token = tokens.Item1,
            RefreshToken = tokens.Item2,
            Expiration = tokenExpiration,
            User = new UserDto
            {
                Id = userResult.Value.Id,
                Email = userResult.Value.Email,
                FirstName = userResult.Value.FirstName,
                LastName = userResult.Value.LastName,
                Role = userResult.Value.Role,
                PhoneNumber = userResult.Value.PhoneNumber
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.GoogleSignedInSuccess,
            "Successfully signed in user via Google: {Email}", payload.Email);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("register")]
    [EndpointSummary("Registers a new SpotRent user account.")]
    [EndpointDescription(
        "Validates the incoming registration payload and creates an admin user with the provided credentials.")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.RegisterAttempt,
            "Registration attempt for email: {Email}", registerRequest?.Email);

        if (registerRequest == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.RegisterInvalidNull,
                "Invalid register data: request is null");
            return BadRequest(new ProblemDetails() { Title = "Invalid register data" });
        }

        if (!ModelState.IsValid)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.RegisterModelInvalid,
                "Invalid register data: model state invalid for email: {Email}", registerRequest.Email);
            return BadRequest(ModelState);
        }

        var user = new User
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            Role = Role.User,
        };

        var registrationRequest = new Services.Auth.RegistrationRequest
        {
            User = user,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Password = registerRequest.Password,
            PhoneNumber = registerRequest.PhoneNumber
        };

        var result = await _authService.RegisterAsync(registrationRequest, cancellationToken);
        result.OnFailure(() =>
                Log(LogLevel.Error, AuthControllerEventIds.RegisterFailed,
                    "Registration failed for email: {Email}. Error: {Error}",
                    registerRequest.Email, result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.RegisterSuccess,
                    "Successfully registered user with email: {Email}", registerRequest.Email));
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("login")]
    [EndpointSummary("Authenticates a user with email and password.")]
    [EndpointDescription("Validates user credentials and returns access plus refresh tokens for the account.")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.LoginAttempt,
            "Login attempt for email: {Email}", request?.Email);

        if (request == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.LoginInvalidNull,
                "Invalid login data: request is null");
            return BadRequest(new ProblemDetails() { Title = $"Invalid user data" });
        }

        var validationResult =
            await _authService.ValidateUserCredentials(request.Email, request.Password, cancellationToken);
        validationResult.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.LoginFailed,
                "Login failed for email: {Email}. Error: {Error}", request.Email,
                validationResult.Error));
        if (validationResult.Failure)
        {
            return Unauthorized(new { Message = validationResult.Error });
        }

        var tokens = await _authService.GenerateTokens(validationResult.Value, cancellationToken);
        var tokenExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"]));
        var response = new LoginResponse
        {
            Token = tokens.Item1,
            RefreshToken = tokens.Item2,
            Expiration = tokenExpiration,
            User = new UserDto
            {
                Id = validationResult.Value.Id,
                Email = validationResult.Value.Email,
                FirstName = validationResult.Value.FirstName,
                LastName = validationResult.Value.LastName,
                Role = validationResult.Value.Role,
                PhoneNumber = validationResult.Value.PhoneNumber
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.LoginSuccess,
            "Successfully logged in user: {Email}", request.Email);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh")]
    [EndpointSummary("Refreshes an access token using a refresh token.")]
    [EndpointDescription(
        "Validates the supplied refresh token, regenerates JWT credentials, and returns updated token metadata.")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshAttempt, "Token refresh attempt");

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshEmpty, "Refresh token is empty");
            return BadRequest(new { message = "Refresh token is required" });
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        result.OnFailure(() =>
            Log(LogLevel.Warning, AuthControllerEventIds.TokenRefreshFailed,
                "Token refresh failed: {Error}", result.Error));
        if (result.Failure)
        {
            return Unauthorized(new { message = result.Error });
        }

        LoginResponse response = new LoginResponse
        {
            Token = result.Value.Token,
            RefreshToken = result.Value.RefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:TokenExpirationMinutes"])),
            User = new UserDto
            {
                Id = result.Value.Id,
                Email = result.Value.Email,
                FirstName = result.Value.FirstName,
                LastName = result.Value.LastName,
            }
        };

        Log(LogLevel.Information, AuthControllerEventIds.TokenRefreshedSuccess,
            "Successfully refreshed token for user ID: {UserId}", result.Value.Id);

        return Ok(response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("logout")]
    [EndpointSummary("Logs out a user by revoking the refresh token.")]
    [EndpointDescription("Ensures a refresh token is provided and invalidates it to end the user session.")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.LogoutAttempt, "Logout attempt");

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.LogoutEmptyToken,
                "Logout failed: refresh token is empty");
            return BadRequest(new { message = "Refresh token is required" });
        }

        var result = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        result.OnFailure(() =>
                Log(LogLevel.Warning, AuthControllerEventIds.LogoutFailed, "Logout failed: {Error}",
                    result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.LogoutSuccess, "Successfully logged out user"));
        if (result.Failure)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("verify")]
    [Authorize(Roles = "User, Owner, Admin")]
    [EndpointSummary("Verifies the caller's JWT and returns profile data.")]
    [EndpointDescription(
        "Reads the user identifier from claims, loads the user entity, and confirms the token is still valid.")]
    public async Task<ActionResult<UserDto>> VerifyToken(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");
            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var id);
        if (isParsed)
        {
            var result = await _authService.GetUserAsync(id, cancellationToken);
            result.OnFailure(() =>
            {
                Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationFailed,
                    "Token verification failed for user ID: {UserId}. Error: {Error}", id, result.Error);
            });
            result.OnSuccess(() =>
            {
                Log(LogLevel.Information, AuthControllerEventIds.TokenVerifiedSuccess,
                    "Successfully verified token for user ID: {UserId}", id);
            });

            if (result.Failure)
            {
                return Unauthorized();
            }

            var userDto = new UserDto
            {
                Id = result.Value.Id,
                Email = result.Value.Email,
                FirstName = result.Value.FirstName,
                LastName = result.Value.LastName,
                Role = result.Value.Role,
                PhoneNumber = result.Value.PhoneNumber
            };

            return Ok(userDto);
        }

        Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationParseFailed,
            "Token verification failed: could not parse user ID");
        return Unauthorized();
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("create-admin")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Creates a new administrator account.")]
    [EndpointDescription("Accepts registration data from an admin user.")]
    public async Task<IActionResult> CreateAdminAsync(RegisterRequest registerRequest,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.CreateAdminAttempt,
            "Admin creation attempt for email: {Email}", registerRequest?.Email);

        if (registerRequest == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.CreateAdminInvalidNull,
                "Invalid admin creation data: request is null");
            return BadRequest(new ProblemDetails() { Title = "Invalid register data" });
        }

        var user = new User
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            Role = Role.Admin,
        };
        var registrationRequest = new Services.Auth.RegistrationRequest
        {
            User = user,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Password = registerRequest.Password,
            PhoneNumber = registerRequest.PhoneNumber
        };
        var result = await _authService.RegisterAsync(registrationRequest, cancellationToken);
        result.OnFailure(() =>
                Log(LogLevel.Error, AuthControllerEventIds.CreateAdminFailed,
                    "Admin creation failed for email: {Email}. Error: {Error}",
                    registerRequest.Email, result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.CreateAdminSuccess,
                    "Successfully created admin with email: {Email}", registerRequest.Email));
        if (result.Failure)
        {
            return StatusCode(500, result.Error);
        }

        return Ok();
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("create-owner")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Creates a new owner account.")]
    [EndpointDescription("Accepts registration data from an owner user.")]
    public async Task<IActionResult> CreateOwnerAsync(RegisterRequest registerRequest,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, AuthControllerEventIds.CreateAdminAttempt,
            "Admin creation attempt for email: {Email}", registerRequest?.Email);

        if (registerRequest == null)
        {
            Log(LogLevel.Warning, AuthControllerEventIds.CreateAdminInvalidNull,
                "Invalid admin creation data: request is null");
            return BadRequest(new ProblemDetails() { Title = "Invalid register data" });
        }

        var user = new User
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            Role = Role.Owner,
        };

        var registrationRequest = new Services.Auth.RegistrationRequest
        {
            User = user,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Password = registerRequest.Password,
            PhoneNumber = registerRequest.PhoneNumber
        };
        var result = await _authService.RegisterAsync(registrationRequest, cancellationToken);
        result.OnFailure(() =>
                Log(LogLevel.Error, AuthControllerEventIds.CreateAdminFailed,
                    "Admin creation failed for email: {Email}. Error: {Error}",
                    registerRequest.Email, result.Error))
            .OnSuccess(() =>
                Log(LogLevel.Information, AuthControllerEventIds.CreateAdminSuccess,
                    "Successfully created admin with email: {Email}", registerRequest.Email));
        if (result.Failure)
        {
            return StatusCode(500, result.Error);
        }

        return Ok();
    }
}
