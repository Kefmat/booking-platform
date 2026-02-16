using System.Security.Claims;
using Booking.Api.Common;
using Booking.Api.Contracts;

namespace Booking.Api.Services.Abstractions;

public interface IBookingService
{
    Task<Result<Booking.Api.Data.Booking>> CreateBookingAsync(
        ClaimsPrincipal user,
        BookingCreateRequest req,
        CancellationToken ct = default);
}
