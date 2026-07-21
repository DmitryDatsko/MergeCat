namespace MergeCat.Options;

public class JwtOptions
{
    public string TokenSecret { get; init; } = string.Empty;
    public string CookieName { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
