using System.Security.Claims;
using System.Text;
using Booking.Api.Contracts;
using Booking.Api.Data;
using Booking.Api.Services;
using Booking.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

//
// -----------------------------
// Infrastruktur / Middleware
// -----------------------------
//

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for lokal frontend (Vite default port 5173).
// Uten dette blokkerer nettleseren API-kall.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("db"));
});

//
// -----------------------------
// Autentisering (JWT)
// -----------------------------
//

// JWT key (krav: minst 32 tegn for HS256)
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

//
// -----------------------------
// Dependency Injection (Services)
// -----------------------------
//

// Registrerer keyBytes som singleton slik at AuthService kan bruke den direkte.
builder.Services.AddSingleton(keyBytes);

// Forretningslogikk-tjenester
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISeedService, SeedService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

//
// -----------------------------
// Oppstart: Migrasjoner
// -----------------------------
//

// Kjør migrasjoner automatisk ved oppstart (kun dev/demo-scenario).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

//
// -----------------------------
// Middleware pipeline
// -----------------------------
//

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();           // CORS må før auth
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

//
// -----------------------------
// Endpoints
// -----------------------------
//

// Seed (dev only)
app.MapPost("/dev/seed", async (ISeedService seedService, CancellationToken ct) =>
{
    var result = await seedService.SeedAsync(ct);

    if (!result.IsSuccess)
        return Results.Problem(
            title: "Seed feilet",
            detail: result.Error,
            statusCode: result.StatusCode
        );

    return Results.Ok(new { seeded = true });
});

// Login
app.MapPost("/auth/login", async (IAuthService authService, LoginRequest req, CancellationToken ct) =>
{
    var result = await authService.LoginAsync(req, ct);

    if (!result.IsSuccess)
        return Results.Problem(
            title: "Innlogging feilet",
            detail: result.Error,
            statusCode: result.StatusCode
        );

    return Results.Ok(result.Value);
});

// Resources (protected)
app.MapGet("/resources", async (IResourceService resourceService, CancellationToken ct) =>
{
    var items = await resourceService.GetActiveResourcesAsync(ct);
    return Results.Ok(items);
}).RequireAuthorization();

//
// -----------------------------
// Booking endpoints (protected)
// -----------------------------
//

// Create booking
// Endepunktet er bevisst "tynt".
// All forretningslogikk ligger i BookingService.
app.MapPost("/bookings", async (
    IBookingService service,
    ClaimsPrincipal user,
    BookingCreateRequest req,
    CancellationToken ct) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? "ukjent";

    if (userIdStr is null)
        return Results.Unauthorized();

    var result = await service.CreateAsync(
        Guid.Parse(userIdStr),
        email,
        req,
        ct);

    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);

    return Results.Created($"/bookings/{result.Value!.Id}", result.Value);
}).RequireAuthorization();


// Henter innlogget brukers bookinger (nyeste først).
// Bruker service i stedet for å gå direkte mot DbContext.
app.MapGet("/bookings/my", async (
    IBookingService service,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (userIdStr is null)
        return Results.Unauthorized();

    var bookings = await service.GetForUserAsync(
        Guid.Parse(userIdStr),
        ct);

    return Results.Ok(bookings);
}).RequireAuthorization();


// Kansellerer en booking (soft-cancel: status endres, ikke sletting).
// Kun eier eller Admin får lov.
// Igjen: selve logikken ligger i service.
app.MapPost("/bookings/{id:guid}/cancel", async (
    Guid id,
    IBookingService service,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var role = user.FindFirstValue(ClaimTypes.Role) ?? "";
    var email = user.FindFirstValue(ClaimTypes.Email) ?? "ukjent";

    if (userIdStr is null)
        return Results.Unauthorized();

    var result = await service.CancelAsync(
        id,
        Guid.Parse(userIdStr),
        role,
        email,
        ct);

    if (!result.IsSuccess)
        return Results.BadRequest(result.Error);

    return Results.Ok(new { cancelled = true });
}).RequireAuthorization();

app.Run();
