using Booking.Api.Common;
using Booking.Api.Data;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Services;

// Seed er dev-hjelp. Result gjør at endepunktet kan returnere tydelig statuskode og melding ved feil.
public sealed class SeedService : ISeedService
{
    private readonly AppDbContext _db;

    public SeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> SeedAsync(CancellationToken ct = default)
    {
        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@demo.no", ct);
        if (admin is null)
        {
            _db.Users.Add(new User
            {
                Email = "admin@demo.no",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = "Admin"
            });
        }
        else
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin");
            admin.Role = "Admin";
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "user@demo.no", ct);
        if (user is null)
        {
            _db.Users.Add(new User
            {
                Email = "user@demo.no",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user"),
                Role = "User"
            });
        }
        else
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("user");
            user.Role = "User";
        }

        if (!await _db.Resources.AnyAsync(r => r.Name == "Møterom A", ct))
            _db.Resources.Add(new Resource { Name = "Møterom A", Description = "4 plasser, skjerm" });

        if (!await _db.Resources.AnyAsync(r => r.Name == "Møterom B", ct))
            _db.Resources.Add(new Resource { Name = "Møterom B", Description = "8 plasser, whiteboard" });

        await _db.SaveChangesAsync(ct);

        return Result.Ok(200);
    }
}
