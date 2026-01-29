namespace Booking.Api.Contracts;

// Booking-requesten inneholder kun input vi trenger for å opprette en booking.
// Validering skjer i endepunktet (og eventuelt med egne validatorer senere).
public record BookingCreateRequest(Guid ResourceId, DateTimeOffset Start, DateTimeOffset End);
