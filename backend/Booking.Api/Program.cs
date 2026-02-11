using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Booking.Api.Domain;


var builder = WebApplication.CreateBuilder(args);

// Swagger brukes for enkel testing og dokumentasjon av API-et
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Databasekobling via Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("db"));
});

// JWT-oppsett for autentisering
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev_super_secret_change_me";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Kjører migrasjoner automatisk ved oppstart (kun dev/demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// Enkel healthcheck brukt av Docker / overvåkning
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Seeder demo-data slik at API-et er brukbart med én gang
app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    if (!await db.Users.AnyAsync(u => u.Email == "admin@demo.no"))
    {
        db.Users.Add(new User { Email = "admin@demo.no", PasswordHash = "admin", Role = "Admin" });
        db.Users.Add(new User { Email = "user@demo.no", PasswordHash = "user", Role = "User" });

        db.Resources.AddRange(
            new Resource { Name = "Møterom A", Description = "4 plasser, skjerm" },
            new Resource { Name = "Møterom B", Description = "8 plasser, whiteboard" }
        );

        await db.SaveChangesAsync();
    }

    return Results.Ok(new { seeded = true });
});

// Innlogging og utstedelse av JWT-token
app.MapPost("/auth/login", async (AppDbContext db, LoginRequest req) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
    if (user is null || user.PasswordHash != req.Password)
        return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
    };

    var creds = new SigningCredentials(
        new SymmetricSecurityKey(keyBytes),
        SecurityAlgorithms.HmacSha256
    );

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds
    );

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new LoginResponse(jwt, user.Role, user.Email));
});

// Henter alle aktive ressurser
app.MapGet("/resources", async (AppDbContext db) =>
{
    return await db.Resources
        .Where(r => r.IsActive)
        .OrderBy(r => r.Name)
        .ToListAsync();
}).RequireAuthorization();

// Oppretter booking med overlapp-sjekk
app.MapPost("/bookings", async (
    AppDbContext db,
    ClaimsPrincipal user,
    BookingCreateRequest req) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? "ukjent";

    if (userId is null)
        return Results.Unauthorized();

    if (req.End <= req.Start)
        return Results.BadRequest("Slutt må være etter start.");

    // Hindrer dobbeltbooking ved overlapp
    var overlap = await db.Bookings.AnyAsync(b =>
        b.ResourceId == req.ResourceId &&
        b.Status == "Created" &&
        BookingRules.Overlapper(req.Start, req.End, b.Start, b.End)
    );

    if (overlap)
        return Results.Conflict("Tidsrommet overlapper med eksisterende booking.");

    var booking = new Booking.Api.Data.Booking
    {
        ResourceId = req.ResourceId,
        UserId = Guid.Parse(userId),
        Start = req.Start,
        End = req.End
    };

    db.Bookings.Add(booking);

    // Logger handlingen for sporbarhet
    db.AuditEvents.Add(new AuditEvent
    {
        ActorEmail = email,
        Action = "CREATE",
        EntityType = "Booking",
        EntityId = booking.Id.ToString()
    });

    await db.SaveChangesAsync();
    return Results.Created($"/bookings/{booking.Id}", booking);
}).RequireAuthorization();

app.Run();
