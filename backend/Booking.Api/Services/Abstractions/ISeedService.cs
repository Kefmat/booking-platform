using Booking.Api.Common;

namespace Booking.Api.Services.Abstractions;

public interface ISeedService
{
    Task<Result> SeedAsync(CancellationToken ct = default);
}
