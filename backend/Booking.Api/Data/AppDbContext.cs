using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Data;

// DbContext er hovedinngangen til databasen.
// Den holder oversikt over alle entiteter og håndterer spørringer,
// endringer og transaksjoner via Entity Framework Core.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Alle brukere i systemet (både admin og vanlige brukere)
    public DbSet<User> Users => Set<User>();

    // Ressurser som kan bookes, f.eks. møterom eller utstyr
    public DbSet<Resource> Resources => Set<Resource>();

    // Bookinger opprettet av brukere
    public DbSet<Booking> Bookings => Set<Booking>();

    // Enkel audit-logg for sporbarhet og feilsøking
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
}

// Representerer en bruker av systemet.
// Passord er lagret i klartekst KUN for demoformål.
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Unik e-postadresse brukt til innlogging
    public string Email { get; set; } = "";

    // Passord (DEV ONLY – må hashes i ekte systemer)
    public string PasswordHash { get; set; } = "";

    // Rolle brukes til autorisasjon i API-et
    // Eksempler: "Admin", "User"
    public string Role { get; set; } = "User";
}

// En ressurs som kan bookes.
// Eksempel: møterom, bil, konsulent
public class Resource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Navn som vises i brukergrensesnittet
    public string Name { get; set; } = "";

    // Valgfri beskrivelse
    public string Description { get; set; } = "";

    // Inaktive ressurser kan ikke bookes
    public bool IsActive { get; set; } = true;
}

// En booking representerer en reservasjon av en ressurs
// innenfor et bestemt tidsrom.
public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Referanse til ressursen som er booket
    public Guid ResourceId { get; set; }
    public Resource? Resource { get; set; }

    // Referanse til brukeren som opprettet bookingen
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Starttidspunkt for bookingen (UTC)
    public DateTimeOffset Start { get; set; }

    // Sluttidspunkt for bookingen (UTC)
    public DateTimeOffset End { get; set; }

    // Status brukes for livssyklus (Created, Cancelled osv.)
    public string Status { get; set; } = "Created";

    // Når bookingen ble opprettet
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// AuditEvent brukes for å logge viktige handlinger i systemet.
// Denne tabellen endres aldri i ettertid (append-only).
public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Hvem som utførte handlingen (e-post)
    public string ActorEmail { get; set; } = "";

    // Hva som ble gjort (CREATE, UPDATE, DELETE)
    public string Action { get; set; } = "";

    // Hvilken type entitet som ble påvirket
    public string EntityType { get; set; } = "";

    // ID til entiteten som ble endret
    public string EntityId { get; set; } = "";

    // Tidspunkt for hendelsen
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
