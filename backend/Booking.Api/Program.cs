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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for lokal frontend
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

// JWT key (krav: minst 32 tegn for HS256)
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key mangler i konfigurasjon.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key må være minst 32 tegn (256 bits) for HS256.");

// Auth
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

// Services (DI)
// Vi registrerer keyBytes som singleton slik at AuthService kan bruke den direkte.
builder.Services.AddSingleton(keyBytes);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISeedService, SeedService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

// Migrasjoner ved oppstart (dev/demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Seed
app.MapPost("/dev/seed", async (ISeedService seedService, CancellationToken ct) =>
{
    await seedService.SeedAsync(ct);
    return Results.Ok(new { seeded = true });
});

// Login
app.MapPost("/auth/login", async (IAuthService authService, LoginRequest req, CancellationToken ct) =>
{
    var res = await authService.LoginAsync(req, ct);
    return res is null ? Results.Unauthorized() : Results.Ok(res);
});

// Resources (protected)
app.MapGet("/resources", async (IResourceService resourceService, CancellationToken ct) =>
{
    var items = await resourceService.GetActiveResourcesAsync(ct);
    return Results.Ok(items);
}).RequireAuthorization();

// Create booking (protected)
app.MapPost("/bookings", async (
    IBookingService bookingService,
    ClaimsPrincipal user,
    BookingCreateRequest req,
    CancellationToken ct) =>
{
    var (ok, error, booking) = await bookingService.CreateBookingAsync(user, req, ct);

    if (!ok)
    {
        // Vi holder responsene eksplisitte for frontend:
        // - overlap => 409
        // - validation => 400
        // - unauthorized => 401
        if (error == "Unauthorized") return Results.Unauthorized();
        if (error != null && error.Contains("overlapper", StringComparison.OrdinalIgnoreCase))
            return Results.Conflict(error);

        return Results.BadRequest(error ?? "Ukjent feil.");
    }

    return Results.Created($"/bookings/{booking!.Id}", booking);
}).RequireAuthorization();

app.Run();
