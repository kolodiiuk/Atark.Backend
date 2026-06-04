using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SpotRent.Api.Dtos.Spaces;
using SpotRent.Api.Logging;
using SpotRent.Domain.Entities;
using SpotRent.Domain.Enums;
using SpotRent.Domain.Extensions;
using SpotRent.Services.Interfaces;
using SpotRent.Services.Spaces;

namespace SpotRent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpacesController : BaseController<SpacesController>
{
    private readonly ISpaceService _spaceService;

    public SpacesController(ISpaceService spaceService, ILogger<SpacesController> logger) : base(logger)
    {
        _spaceService = spaceService;
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    [EndpointSummary("Creates a new space")]
    [EndpointDescription(
        "Validates the provided space payload, maps it to a domain entity, and persists the new space.")]
    public async Task<IActionResult> CreateSpace(CreateSpaceDto spaceDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.CreateSpaceAttempt,
            "Create space attempt");

        if (spaceDto is null)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.CreateSpaceInvalid,
                "Space payload is null");

            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Payload is not valid" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(userId, out var id);
        if (isParsed)
        {
            var resolvedAddressId = spaceDto.AddressId;
            if (spaceDto.Address is not null)
            {
                var addressResult =
                    await _spaceService.GetOrCreateAddressAsync(spaceDto.Address.MapToAddress(), cancellationToken);
                if (addressResult.Failure)
                {
                    var addressStatus = addressResult.Error?.Contains("required", StringComparison.OrdinalIgnoreCase) ==
                                        true
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError;
                    return StatusCode(addressStatus, new ProblemDetails { Detail = addressResult.Error });
                }

                resolvedAddressId = addressResult.Value.Id;
            }

            if (resolvedAddressId < 1)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    new ProblemDetails
                    {
                        Detail = "Address information is required. Provide a valid addressId or address payload."
                    });
            }

            var space = spaceDto.MapToSpace();
            space.AddressId = resolvedAddressId;
            space.CreatedAt = DateTime.UtcNow;
            space.OwnerId = id;
            var result = await _spaceService.CreateSpaceAsync(space, cancellationToken);
            result.OnSuccess(() =>
                    Log(LogLevel.Information, SpacesControllerEventIds.CreateSpaceSuccess,
                        "Successfully created space {SpaceId}", result.Value.Id))
                .OnFailure(() =>
                    Log(LogLevel.Error, SpacesControllerEventIds.CreateSpaceFailure,
                        "Failed to create space. Error: {Error}", result.Error));

            return result.Failure
                ? StatusCode(IsBadRequestError(result.Error)
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError,
                    new ProblemDetails { Detail = result.Error })
                : StatusCode(StatusCodes.Status201Created, new { result.Value.Id });
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("address")]
    [EndpointSummary("Creates or reuses an address")]
    [EndpointDescription("Finds an existing matching address by building/street/city/region, otherwise creates it.")]
    public async Task<IActionResult> GetOrCreateAddress(CreateAddressDto addressDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (addressDto is null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Payload is not valid" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var result = await _spaceService.GetOrCreateAddressAsync(addressDto.MapToAddress(), cancellationToken);
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = result.Error });
        }

        return StatusCode(StatusCodes.Status200OK, AddressDto.Map(result.Value));
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("attributes")]
    [EndpointSummary("Gets available attributes")]
    [EndpointDescription("Returns the list of space attributes for creating attribute values.")]
    public async Task<IActionResult> GetAttributes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _spaceService.GetAttributesAsync(cancellationToken);
        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = result.Error });
        }

        return StatusCode(StatusCodes.Status200OK, result.Value.Select(AttributeDto.Map).ToList());
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [EndpointSummary("Filters spaces")]
    [EndpointDescription("Supports filtering by attributes such as type, capacity, rates, and city while honoring pagination and sorting parameters.")]
    public async Task<IActionResult> GetSpaces(
        [FromQuery(Name = "spaceType")] SpaceType? spaceType,
        [FromQuery(Name = "minCapacity")] int? minCapacity,
        [FromQuery(Name = "maxCapacity")] int? maxCapacity,
        [FromQuery(Name = "minAreaSqm")] double? minAreaSqm,
        [FromQuery(Name = "maxAreaSqm")] double? maxAreaSqm,
        [FromQuery(Name = "minHourlyRate")] decimal? minHourlyRate,
        [FromQuery(Name = "maxHourlyRate")] decimal? maxHourlyRate,
        [FromQuery(Name = "city")] string city,
        [FromQuery(Name = "attributes")] string attributes,
        [FromQuery(Name = "sort")] string sort,
        CancellationToken cancellationToken,
        [FromQuery(Name = "limit")] int limit = 50,
        [FromQuery(Name = "offset")] int offset = 0)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.GetSpacesAttempt,
            "Get spaces attempt with limit {Limit} and offset {Offset}", limit, offset);

        if (limit <= 0 || offset < 0)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.GetSpacesInvalid,
                "Invalid pagination parameters supplied");

            return StatusCode(StatusCodes.Status400BadRequest,
                new ProblemDetails { Detail = "Pagination parameters are invalid" });
        }

        if ((minCapacity.HasValue && maxCapacity.HasValue && minCapacity > maxCapacity) ||
            (minAreaSqm.HasValue && maxAreaSqm.HasValue && minAreaSqm > maxAreaSqm) ||
            (minHourlyRate.HasValue && maxHourlyRate.HasValue && minHourlyRate > maxHourlyRate))
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.GetSpacesInvalid,
                "Invalid range parameters supplied");

            return StatusCode(StatusCodes.Status400BadRequest,
                new ProblemDetails { Detail = "Range parameters are invalid" });
        }

        limit = Math.Min(limit, 100);

        var criteria = new SpaceFilterCriteria
        {
            SpaceType = spaceType,
            MinCapacity = minCapacity,
            MaxCapacity = maxCapacity,
            MinAreaSqm = minAreaSqm,
            MaxAreaSqm = maxAreaSqm,
            MinHourlyRate = minHourlyRate,
            MaxHourlyRate = maxHourlyRate,
            City = string.IsNullOrWhiteSpace(city) ? null : city,
            Attributes = attributes
        };

        var predicate = AttributeHelper.BuildPredicate(criteria);
        var includes = BuildDefaultIncludes();
        var orderBy = AttributeHelper.BuildOrderByDelegate(sort);

        var totalItems = 0;
        var request = new SpaceFilterRequest
        {
            Predicate = predicate,
            Includes = includes,
            OrderBy = orderBy,
            SkipCount = offset,
            TakeCount = limit,
            CaptureTotal = count => totalItems = count
        };

        var result = await _spaceService.FilterSpacesAsync(request, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, SpacesControllerEventIds.GetSpacesSuccess,
                    "Successfully retrieved spaces"))
            .OnFailure(() =>
                Log(LogLevel.Error, SpacesControllerEventIds.GetSpacesFailure,
                    "Failed to retrieve spaces. Error: {Error}", result.Error));

        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = result.Error });
        }

        var page = offset / limit + 1;
        var spaces = result.Value.Select(SpaceDto.MapSpace).ToList();
        var response = new PagedResult<SpaceDto>(spaces, totalItems, page, limit);

        return StatusCode(StatusCodes.Status200OK, response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{id:int}")]
    [EndpointSummary("Gets a single space by ID")]
    [EndpointDescription("Validates the space ID, loads the space with related data, and returns it or appropriate status when missing.")]
    public async Task<IActionResult> GetSpace(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.GetSpaceAttempt,
            "Get space attempt for ID {Id}", id);

        if (id < 1)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.GetSpaceInvalid,
                "Space ID is invalid");
            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Id is not valid" });
        }

        var result = await _spaceService.GetSpaceByIdAsync(id, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, SpacesControllerEventIds.GetSpaceSuccess,
                    "Successfully retrieved space"))
            .OnFailure(() =>
                Log(LogLevel.Error, SpacesControllerEventIds.GetSpaceFailure,
                    "Failed to retrieve space. Error: {Error}", result.Error));

        if (result.Failure)
        {
            var status = result.Error?.StartsWith("No space", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status500InternalServerError;

            return StatusCode(status, new ProblemDetails { Detail = result.Error });
        }

        var space = SpaceDto.MapSpace(result.Value);

        return StatusCode(StatusCodes.Status200OK, space);
    }

    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Lists available spaces for a time range in a city")]
    [EndpointDescription("Checks the requested city and dates (required), queries availability, and returns spaces free during the specified interval.")]
    public async Task<IActionResult> GetAvailableSpaces(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] string city,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.GetAvailableSpacesAttempt,
            "Get available spaces attempt between {Start} and {End} for {City}",
            startTime, endTime, city);

        if (endTime <= startTime || string.IsNullOrWhiteSpace(city))
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.GetAvailableSpacesInvalid,
                "Invalid availability query payload");
            return StatusCode(StatusCodes.Status400BadRequest,
                new ProblemDetails { Detail = "City and date range must be provided and valid" });
        }

        var result = await _spaceService.GetAvailableSpacesAsync(startTime, endTime, city, cancellationToken);

        result.OnSuccess(() =>
                Log(LogLevel.Information, SpacesControllerEventIds.GetAvailableSpacesSuccess,
                    "Successfully retrieved available spaces"))
            .OnFailure(() =>
                Log(LogLevel.Error, SpacesControllerEventIds.GetAvailableSpacesFailure,
                    "Failed to retrieve available spaces. Error: {Error}", result.Error));

        if (result.Failure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = result.Error });
        }

        var response = result.Value.Select(SpaceDto.MapSpace);
        return StatusCode(StatusCodes.Status200OK, response);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{id:int}/schedule")]
    [EndpointSummary("Gets the booking schedule for a space")]
    [EndpointDescription("Validates identifiers and date range, then returns the calendar of bookings for the requested space.")]
    public async Task<IActionResult> GetSpaceSchedule(int id, [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.GetSpaceScheduleAttempt,
            "Get space schedule attempt for ID {Id}", id);

        if (id < 1)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.GetSpaceScheduleInvalid,
                "Space ID is invalid");

            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Id is not valid" });
        }

        var rangeStart = startDate ?? DateTime.UtcNow.Date;
        var rangeEnd = endDate ?? rangeStart.AddDays(7);

        if (rangeEnd <= rangeStart)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                new ProblemDetails { Detail = "End date must be after start date" });
        }

        var result = await _spaceService.GetSpaceScheduleAsync(id, rangeStart, rangeEnd, cancellationToken);
        result.OnSuccess(() =>
                Log(LogLevel.Information, SpacesControllerEventIds.GetSpaceScheduleSuccess,
                    "Successfully retrieved space schedule"))
            .OnFailure(() =>
                Log(LogLevel.Error, SpacesControllerEventIds.GetSpaceScheduleFailure,
                    "Failed to retrieve space schedule. Error: {Error}", result.Error));

        if (result.Failure)
        {
            var status = result.Error?.StartsWith("No space", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status500InternalServerError;

            return StatusCode(status, new ProblemDetails { Detail = result.Error });
        }

        return StatusCode(StatusCodes.Status200OK, result.Value);
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("owner")]
    [EndpointSummary("Gets owner's spaces")]
    [EndpointDescription("Gets spaces of currently authorised owner.")]
    public async Task<IActionResult> GetOwnerSpacesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (UserId is null)
        {
            return Unauthorized();
        }

        var res = await _spaceService.GetOwnerSpacesAsync(UserId.Value, ct);
        res.OnFailure(() => Log(LogLevel.Warning, new EventId(7777), res.Error));

        return res.IsSuccess switch
        {
            true => Ok(res.Value.Select(SpaceDto.MapSpace)),
            _ => Problem(title: "Problem retrieving spaces", detail: res.Error,
                statusCode: StatusCodes.Status400BadRequest)
        };
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPatch("{id:int}")]
    [EndpointSummary("Updates an existing space")]
    [EndpointDescription("Accepts the edited space details, validates identifiers, and updates the stored space record.")]
    public async Task<IActionResult> UpdateSpace(int id, UpdateSpaceDto spaceDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.UpdateSpaceAttempt,
            "Update space attempt for ID {Id}", id);

        if (id < 1 || spaceDto is null)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.UpdateSpaceInvalid,
                "Space update payload invalid");

            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Payload is not valid" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");

            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var resolvedAddressId = spaceDto.AddressId;
            if (spaceDto.Address is not null)
            {
                var addressResult =
                    await _spaceService.GetOrCreateAddressAsync(spaceDto.Address.MapToAddress(), cancellationToken);
                if (addressResult.Failure)
                {
                    var addressStatus = addressResult.Error?.Contains("required", StringComparison.OrdinalIgnoreCase) ==
                                        true
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError;
                    return StatusCode(addressStatus, new ProblemDetails { Detail = addressResult.Error });
                }

                resolvedAddressId = addressResult.Value.Id;
            }

            if (resolvedAddressId < 1)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    new ProblemDetails
                    {
                        Detail = "Address information is required. Provide a valid addressId or address payload."
                    });
            }

            var space = spaceDto.MapToSpace(id);
            space.Id = id;
            space.AddressId = resolvedAddressId;
            var result = await _spaceService.UpdateSpaceAsync(space, parsedUserId, cancellationToken);
            result.OnSuccess(() =>
                    Log(LogLevel.Information, SpacesControllerEventIds.UpdateSpaceSuccess,
                        "Successfully updated space {SpaceId}", id))
                .OnFailure(() =>
                    Log(LogLevel.Error, SpacesControllerEventIds.UpdateSpaceFailure,
                        "Failed to update space {SpaceId}. Error: {Error}", id, result.Error));

            if (result.Failure)
            {
                var status = result.Error?.StartsWith("No space", StringComparison.OrdinalIgnoreCase) == true
                    ? StatusCodes.Status404NotFound
                    : IsBadRequestError(result.Error)
                        ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;

                return StatusCode(status, new ProblemDetails { Detail = result.Error });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    [Authorize(Roles = "Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("{id:int}")]
    [EndpointSummary("Deletes a space")]
    [EndpointDescription("Validates the provided space identifier and removes the associated space if it exists.")]
    public async Task<IActionResult> DeleteSpace(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log(LogLevel.Information, SpacesControllerEventIds.DeleteSpaceAttempt,
            "Delete space attempt for ID {Id}", id);

        if (id < 1)
        {
            Log(LogLevel.Warning, SpacesControllerEventIds.DeleteSpaceInvalid,
                "Space ID is invalid");

            return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails { Detail = "Id is not valid" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log(LogLevel.Information, AuthControllerEventIds.TokenVerificationAttempt,
            "Token verification attempt for user ID: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Log(LogLevel.Warning, AuthControllerEventIds.TokenVerificationNoUserId,
                "Token verification failed: user ID not found in claims");

            return Unauthorized();
        }

        var isParsed = int.TryParse(userId, out var parsedUserId);
        if (isParsed)
        {
            var result = await _spaceService.DeleteSpaceAsync(id, parsedUserId, cancellationToken);
            result.OnSuccess(() =>
                    Log(LogLevel.Information, SpacesControllerEventIds.DeleteSpaceSuccess,
                        "Successfully deleted space {SpaceId}", id))
                .OnFailure(() =>
                    Log(LogLevel.Error, SpacesControllerEventIds.DeleteSpaceFailure,
                        "Failed to delete space {SpaceId}. Error: {Error}", id, result.Error));

            if (result.Failure)
            {
                var status = result.Error?.StartsWith("No space", StringComparison.OrdinalIgnoreCase) == true
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status500InternalServerError;

                return StatusCode(status, new ProblemDetails() { Detail = result.Error });
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }

        return StatusCode(StatusCodes.Status401Unauthorized);
    }

    private static Func<IQueryable<Space>, IIncludableQueryable<Space, object>> BuildDefaultIncludes() =>
        query => query
            .Include(s => s.Address)
            .Include(s => s.Owner)
            .Include(s => s.WorkingHours)
            .Include(s => s.AttributeValues)
            .ThenInclude(av => av.Attribute);

    private static bool IsBadRequestError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("expects", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("required", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("unknown attributeid", StringComparison.OrdinalIgnoreCase);
    }
}
