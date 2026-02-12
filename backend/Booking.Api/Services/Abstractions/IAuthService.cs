using Booking.Api.Contracts;

namespace Booking.Api.Services.Abstractions;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default);
}
