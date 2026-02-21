using Booking.Api.Common;
using Booking.Api.Contracts;

// Sørger for at "BookingEntity" alltid peker på EF-entityen
using BookingEntity = Booking.Api.Data.Booking;

namespace Booking.Api.Services.Abstractions;

public interface IBookingService
{
    // Oppretter booking (med overlap-sjekk + audit)
    Task<Result<BookingEntity>> CreateAsync(
        Guid userId,
        string actorEmail,
        BookingCreateRequest req,
        CancellationToken ct);

    // Henter innlogget brukers bookinger
    Task<IReadOnlyList<BookingEntity>> GetForUserAsync(
        Guid userId,
        CancellationToken ct);

    // Kansellerer booking hvis eier eller admin
    Task<Result> CancelAsync(
        Guid bookingId,
        Guid callerUserId,
        string callerRole,
        string actorEmail,
        CancellationToken ct);
}