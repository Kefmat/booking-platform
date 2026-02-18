using Booking.Api.Common;
using Booking.Api.Contracts;

namespace Booking.Api.Services.Abstractions;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default);
}
