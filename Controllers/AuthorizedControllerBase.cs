using System.IdentityModel.Tokens.Jwt;
using MergeCat.Models;
using Microsoft.AspNetCore.Mvc;

namespace MergeCat.Controllers;

[Route("api/[controller]")]
public abstract class AuthorizedControllerBase : ControllerBase
{
    protected string CurrentAddress => EthereumAddress.Parse(User.FindFirst("address")!.Value);

    protected Guid CurrentPlayerId =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
}
