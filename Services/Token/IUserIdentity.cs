using System.Security.Claims;

namespace MergeCat.Services.Token;

public interface IUserIdentity
{
    string? GetAddress(ClaimsPrincipal? principal);
}
