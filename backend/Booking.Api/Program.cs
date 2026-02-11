using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Swagger brukes for enkel testing og dokumentasjon av API-et
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tillater at frontend (Vite på localhost:5173) kan kalle API-et lokalt i dev.
// Uten dette vil nettleseren blokkere kall (CORS) og frontend får ofte "Failed to fetch".
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Databasekobling via Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("db"));
});

// JWT-oppsett for autentisering
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key mangler i konfigurasjon.");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key må være minst 32 tegn (256 bits) for HS256.");

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

// CORS må ligge før auth, ellers kan preflight/requests bli blokkert i nettleser
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Enkel healthcheck brukt av Docker / overvåkning
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Seeder demo-data slik at API-et er brukbart med én gang
// I dev vil vi at dette skal være "trygt å kjøre flere ganger" (idempotent).
app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    // Opprett eller oppdater demo-brukere
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@demo.no");
    if (admin is null)
    {
        db.Users.Add(new User { Email = "admin@demo.no", PasswordHash = "admin", Role = "Admin" });
    }
    else
    {
        // Sørger for at demo-login alltid fungerer
        admin.PasswordHash = "admin";
        admin.Role = "Admin";
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "user@demo.no");
    if (user is null)
    {
        db.Users.Add(new User { Email = "user@demo.no", PasswordHash = "user", Role = "User" });
    }
    else
    {
        user.PasswordHash = "user";
        user.Role = "User";
    }

    // Opprett ressurser hvis de mangler
    if (!await db.Resources.AnyAsync(r => r.Name == "Møterom A"))
        db.Resources.Add(new Resource { Name = "Møterom A", Description = "4 plasser, skjerm" });

    if (!await db.Resources.AnyAsync(r => r.Name == "Møterom B"))
        db.Resources.Add(new Resource { Name = "Møterom B", Description = "8 plasser, whiteboard" });

    await db.SaveChangesAsync();
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

    // Bruker full namespace her for å unngå navnekollisjon (Booking kan lett bli tolket som namespace)
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
