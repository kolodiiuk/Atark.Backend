using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Spaces;
using Attribute = SpotRent.Domain.Entities.Attribute;

namespace SpotRent.Services.Interfaces;

public interface ISpaceService
{
    Task<Result<IEnumerable<Space>>> FilterSpacesAsync(SpaceFilterRequest req, CancellationToken cancellationToken);

    Task<Result<Space>> GetSpaceByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Space>>> GetAvailableSpacesAsync(DateTime startTime, DateTime endTime, string city,
        CancellationToken cancellationToken);

    Task<Result<Space>> CreateSpaceAsync(Space space, CancellationToken cancellationToken);

    Task<Result> UpdateSpaceAsync(Space space, int ownerId, CancellationToken cancellationToken);

    Task<Result> DeleteSpaceAsync(int id, int ownerId, CancellationToken cancellationToken);

    Task<Result<bool>> IsSpaceAvailableAsync(int workspaceId, DateTime startTime, DateTime endTime,
        CancellationToken cancellationToken);

    Task<Result<SpaceSchedule>> GetSpaceScheduleAsync(int spaceId, DateTime? startDate, DateTime? endDate,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<Space>>> GetOwnerSpacesAsync(int ownerId, CancellationToken ct);

    Task<Result<Address>> GetOrCreateAddressAsync(Address address, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Attribute>>> GetAttributesAsync(CancellationToken cancellationToken);
}
