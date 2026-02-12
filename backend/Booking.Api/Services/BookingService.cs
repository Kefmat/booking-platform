using System.Security.Claims;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Domain;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Services;

// Samler booking-regler + opprettelse + audit logging.
// Endepunktet blir bare en "routing"-wrapper.
public sealed class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool ok, string? error, Booking.Api.Data.Booking? booking)> CreateBookingAsync(
        ClaimsPrincipal user,
        BookingCreateRequest req,
        CancellationToken ct = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = user.FindFirstValue(ClaimTypes.Email) ?? "ukjent";

        if (userId is null)
            return (false, "Unauthorized", null);

        if (req.End <= req.Start)
            return (false, "Slutt må være etter start.", null);

        var overlap = await _db.Bookings.AnyAsync(b =>
            b.ResourceId == req.ResourceId &&
            b.Status == "Created" &&
            BookingRules.Overlapper(req.Start, req.End, b.Start, b.End), ct);

        if (overlap)
            return (false, "Tidsrommet overlapper med eksisterende booking.", null);

        var booking = new Booking.Api.Data.Booking
        {
            ResourceId = req.ResourceId,
            UserId = Guid.Parse(userId),
            Start = req.Start,
            End = req.End
        };

        _db.Bookings.Add(booking);

        _db.AuditEvents.Add(new AuditEvent
        {
            ActorEmail = email,
            Action = "CREATE",
            EntityType = "Booking",
            EntityId = booking.Id.ToString()
        });

        await _db.SaveChangesAsync(ct);
        return (true, null, booking);
    }
}
