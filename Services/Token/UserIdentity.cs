using System.Security.Claims;

namespace MergeCat.Services.Token;

public class UserIdentity : IUserIdentity
{
    public string? GetAddress(ClaimsPrincipal? principal) => principal?.FindFirst("address")?.Value;
}
