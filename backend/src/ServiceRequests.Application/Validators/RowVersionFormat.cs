namespace ServiceRequests.Application.Validators;

public static class RowVersionFormat
{
    // SQL Server rowversion is always 8 bytes.
    private const int RowVersionLength = 8;

    public const string ErrorMessage = "'rowVersion' must be the Base64 value returned by the API.";

    public static bool IsValid(string? rowVersion)
    {
        Span<byte> buffer = stackalloc byte[RowVersionLength];
        return Convert.TryFromBase64String(rowVersion ?? string.Empty, buffer, out var bytesWritten)
            && bytesWritten == RowVersionLength;
    }
}
