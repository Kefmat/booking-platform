using Booking.Api.Data;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Services;

// Egen service for ressurser gjør det lett å legge til caching/filter senere.
public sealed class ResourceService : IResourceService
{
    private readonly AppDbContext _db;

    public ResourceService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Resource>> GetActiveResourcesAsync(CancellationToken ct = default)
    {
        return _db.Resources
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }
}
