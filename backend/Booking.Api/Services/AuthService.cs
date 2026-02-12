using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Booking.Api.Services;

// Samler all autentiseringslogikk på ett sted.
// Da slipper vi "JWT-bygging" og passordsjekk inni endepunkter.
public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly byte[] _keyBytes;

    public AuthService(AppDbContext db, byte[] keyBytes)
    {
        _db = db;
        _keyBytes = keyBytes;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
        if (user is null) return null;

        // Passord sjekkes med BCrypt (aldri plain text sammenligning).
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return null;

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
        return new LoginResponse(jwt, user.Role, user.Email);
    }
}
