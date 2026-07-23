using System.IdentityModel.Tokens.Jwt;
using MergeCat.Models;
using Microsoft.AspNetCore.Mvc;

namespace MergeCat.Controllers;

public abstract class AuthorizedControllerBase : ControllerBase
{
    protected string CurrentAddress => EthereumAddress.Parse(User.FindFirst("address")!.Value);

    protected Guid CurrentPlayerId =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    protected Player? CurrentPlayer =>
        HttpContext.Items.TryGetValue(nameof(Player), out var p) ? p as Player : null;
}
