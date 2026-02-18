namespace Booking.Api.Common;

// En enkel Result-type som gjør at services kan returnere
// enten "Ok" med data, eller "Fail" med feilmelding + statuskode.
// Dette gjør at endepunktene slipper masse if/else-logikk.
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, int StatusCode)
{
    public static Result<T> Ok(T value, int statusCode = 200)
        => new(true, value, null, statusCode);

    public static Result<T> Fail(string error, int statusCode)
        => new(false, default, error, statusCode);
}

// Variant for operasjoner som ikke returnerer data.
public sealed record Result(bool IsSuccess, string? Error, int StatusCode)
{
    public static Result Ok(int statusCode = 200)
        => new(true, null, statusCode);

    public static Result Fail(string error, int statusCode)
        => new(false, error, statusCode);
}
