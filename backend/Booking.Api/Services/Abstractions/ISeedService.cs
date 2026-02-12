namespace Booking.Api.Services.Abstractions;

public interface ISeedService
{
    Task SeedAsync(CancellationToken ct = default);
}
