namespace Booking.Api.Domain;

public static class BookingRules
{
    // Returnerer true hvis to tidsintervaller overlapper.
    // Brukes for å hindre dobbeltbooking.
    //
    // Overlap-regel:
    //   start1 < slutt2  AND  slutt1 > start2
    public static bool Overlapper(DateTimeOffset start1, DateTimeOffset slutt1, DateTimeOffset start2, DateTimeOffset slutt2)
    {
        return start1 < slutt2 && slutt1 > start2;
    }
}
