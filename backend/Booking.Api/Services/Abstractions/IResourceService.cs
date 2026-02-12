using Booking.Api.Data;

namespace Booking.Api.Services.Abstractions;

public interface IResourceService
{
    Task<List<Resource>> GetActiveResourcesAsync(CancellationToken ct = default);
}
