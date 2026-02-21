using Booking.Api.Common;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Domain;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

// Unngår at "Booking" tolkes som namespace
using BookingEntity = Booking.Api.Data.Booking;

namespace Booking.Api.Services;

public sealed class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BookingEntity>> CreateAsync(
        Guid userId,
        string actorEmail,
        BookingCreateRequest req,
        CancellationToken ct)
    {
        // Enkel input-validering (API-validering finnes også, men services bør være defensive)
        if (req.ResourceId == Guid.Empty)
            return Result<BookingEntity>.Failure("RessursId mangler.", 400);

        if (req.End <= req.Start)
            return Result<BookingEntity>.Failure("Slutt må være etter start.", 400);

        // Sjekk at ressurs finnes og er aktiv
        var resourceExists = await _db.Resources.AnyAsync(r => r.Id == req.ResourceId && r.IsActive, ct);
        if (!resourceExists)
            return Result<BookingEntity>.Failure("Ressurs finnes ikke (eller er deaktivert).", 404);

        // Hindrer dobbeltbooking ved overlapp
        var overlap = await _db.Bookings.AnyAsync(b =>
            b.ResourceId == req.ResourceId &&
            b.Status == "Created" &&
            BookingRules.Overlapper(req.Start, req.End, b.Start, b.End), ct);

        if (overlap)
            return Result<BookingEntity>.Failure("Tidsrommet overlapper med eksisterende booking.", 409);

        var booking = new BookingEntity
        {
            ResourceId = req.ResourceId,
            UserId = userId,
            Start = req.Start,
            End = req.End,
            Status = "Created",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Bookings.Add(booking);

        // Audit: logg hvem som gjorde hva (nyttig for debugging og "sporbarhet")
        _db.AuditEvents.Add(new AuditEvent
        {
            ActorEmail = actorEmail,
            Action = "CREATE",
            EntityType = "Booking",
            EntityId = booking.Id.ToString(),
            At = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Result<BookingEntity>.Success(booking, 201);
    }

    public async Task<IReadOnlyList<BookingEntity>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Start)
            .ToListAsync(ct);
    }

    public async Task<Result> CancelAsync(
        Guid bookingId,
        Guid callerUserId,
        string callerRole,
        string actorEmail,
        CancellationToken ct)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null)
            return Result.Failure("Fant ikke booking.", 404);

        var isOwner = booking.UserId == callerUserId;
        var isAdmin = string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isAdmin)
            return Result.Failure("Du har ikke tilgang til å kansellere denne bookingen.", 403);

        // Hvis den allerede er kansellert, returnerer vi OK likevel
        if (booking.Status == "Cancelled")
            return Result.Success(200);

        booking.Status = "Cancelled";

        _db.AuditEvents.Add(new AuditEvent
        {
            ActorEmail = actorEmail,
            Action = "CANCEL",
            EntityType = "Booking",
            EntityId = booking.Id.ToString(),
            At = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Result.Success(200);
    }
}