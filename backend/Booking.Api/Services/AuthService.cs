using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Booking.Api.Common;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Booking.Api.Services;

// Autentisering samles her slik at Program.cs bare blir "wiring" + routing.
// Result-pattern gjør at vi kan returnere riktig statuskode uten masse if/else i endepunktet.
public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly byte[] _keyBytes;

    public AuthService(AppDbContext db, byte[] keyBytes)
    {
        _db = db;
        _keyBytes = keyBytes;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        // Bevisst samme feilmelding uansett om e-post eller passord er feil
        // for å ikke "lekke" hvilke brukere som finnes (enkelt tiltak mot user-enumeration).
        const string invalid = "Ugyldig e-post eller passord.";

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
        if (user is null)
            return Result<LoginResponse>.Fail(invalid, 401);

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Result<LoginResponse>.Fail(invalid, 401);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(_keyBytes),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Result<LoginResponse>.Ok(new LoginResponse(jwt, user.Role, user.Email), 200);
    }
}
