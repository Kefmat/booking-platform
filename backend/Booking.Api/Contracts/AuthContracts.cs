namespace Booking.Api.Contracts;

// API-kontrakter holdes separert fra database-entiteter.
// Det gjør at vi kan endre databasen uten å "knekke" API-et (og omvendt).

public record LoginRequest(string Email, string Password);

// Vi returnerer bare det klienten trenger her.
// Ikke send hele "User"-entiteten fra databasen ut til frontend.
public record LoginResponse(string Token, string Role, string Email);
