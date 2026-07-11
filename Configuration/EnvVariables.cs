namespace MergeCat.Configuration;

public class EnvVariables
{
    public string JwtTokenSecret { get; set; } = string.Empty;
    public string CookieName { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
