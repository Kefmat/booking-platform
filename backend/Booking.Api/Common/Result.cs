namespace Booking.Api.Common;

// En enkel Result-type som gjør at services kan returnere
// enten "Ok" med data, eller "Fail" med feilmelding + statuskode.
// Dette gjør at endepunktene slipper masse if/else-logikk.
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, int StatusCode)
{
    // Standard navn (kort og enkelt)
    public static Result<T> Ok(T value, int statusCode = 200)
        => new(true, value, null, statusCode);

    public static Result<T> Fail(string error, int statusCode)
        => new(false, default, error, statusCode);

    // Alias-navn (mer “enterprise”-aktig) – nyttig hvis du allerede bruker disse
    public static Result<T> Success(T value, int statusCode = 200)
        => Ok(value, statusCode);

    public static Result<T> Failure(string error, int statusCode)
        => Fail(error, statusCode);
}

// Variant for operasjoner som ikke returnerer data.
public sealed record Result(bool IsSuccess, string? Error, int StatusCode)
{
    public static Result Ok(int statusCode = 200)
        => new(true, null, statusCode);

    public static Result Fail(string error, int statusCode)
        => new(false, error, statusCode);

    // Alias-navn for samme grunn som over
    public static Result Success(int statusCode = 200)
        => Ok(statusCode);

    public static Result Failure(string error, int statusCode)
        => Fail(error, statusCode);
}