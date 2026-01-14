using SpotRent.Domain.Common;
using SpotRent.Domain.Entities;
using SpotRent.Services.Bookings;

namespace SpotRent.Services.Interfaces;

public interface IBookingService
{
    Task<Result<BookingCreationResponse>> CreateBookingAsync(int userId, CreateBookingRequest req,
        CancellationToken cancellationToken);

    Task<Result<IEnumerable<Booking>>> GetUserBookingsHistoryAsync(int userId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Booking>>> GetUserActiveBookingsAsync(int userId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Booking>>> GetOwnerBookingsAsync(int ownerId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Booking>>> GetOwnerActiveBookingsAsync(int ownerId, CancellationToken cancellationToken);

    Task<Result<IEnumerable<Booking>>> GetBookingsAsync(int requesterId, BookingFilterRequest req,
        CancellationToken cancellationToken);

    Task<Result<Booking>> GetBookingByIdAsync(int id, int requesterId, CancellationToken cancellationToken);

    Task<Result> CancelBookingAsync(int bookingId, CancellationToken cancellationToken);
}
