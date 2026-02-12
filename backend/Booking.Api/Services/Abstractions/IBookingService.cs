using System.Security.Claims;
using Booking.Api.Contracts;

namespace Booking.Api.Services.Abstractions;

public interface IBookingService
{
    Task<(bool ok, string? error, Booking.Api.Data.Booking? booking)> CreateBookingAsync(
        ClaimsPrincipal user,
        BookingCreateRequest req,
        CancellationToken ct = default);
}
