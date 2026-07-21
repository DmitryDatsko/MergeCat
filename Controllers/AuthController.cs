using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MergeCat.Context;
using MergeCat.Models;
using MergeCat.Models.DTO;
using MergeCat.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Signer;

namespace MergeCat.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    IOptions<JwtOptions> jwtOptions,
    IOptions<GameOptions> gameOptions,
    IMemoryCache cache,
    AppDbContext db
) : AuthorizedControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly GameOptions _gameOptions = gameOptions.Value;
    private readonly IMemoryCache _cache = cache;

    [HttpPost("verify")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticationRequest request)
    {
        var nonce = ExtractNonceFromMessage(request.Message);
        if (string.IsNullOrEmpty(nonce))
            return Unauthorized(new { message = "Nonce not found in message" });

        if (!_cache.TryGetValue(nonce, out _))
            return Unauthorized(new { message = "Nonce is expired or doesn't exist" });

        _cache.Remove(nonce);

        var signer = new EthereumMessageSigner();
        var recoveredAddress = signer.EncodeUTF8AndEcRecover(request.Message, request.Signature);

        if (string.IsNullOrEmpty(recoveredAddress))
            return Unauthorized(new { message = "Invalid or expired nonce" });

        var normalizedAddress = EthereumAddress.Parse(recoveredAddress);
        var player = await db.Players.FirstOrDefaultAsync(p =>
            p.WalletAddress == normalizedAddress
        );

        if (player is null)
        {
            player = new Player
            {
                WalletAddress = normalizedAddress,
                Balance = _gameOptions.StartingBalance,
                TotalEarned = 0,
                IncomeRate = 0,
                LastCollectedAt = DateTime.UtcNow,
            };

            db.Players.Add(player);
            await db.SaveChangesAsync();

            var cells = Enumerable
                .Range(0, _gameOptions.BoardSize)
                .Select(i => new Cell
                {
                    PlayerId = player.Id,
                    Index = i,
                    UnitLevel = 0,
                });

            db.Cells.AddRange(cells);
            await db.SaveChangesAsync();
        }

        var accessToken = CreateToken(player);
        SetCookie(accessToken, HttpContext);

        return Ok(new { message = "Authentication successful" });
    }

    [HttpGet("nonce")]
    public async Task<IActionResult> Nonce()
    {
        var nonce = GenerateSecureNonce();

        _cache.Set(
            nonce,
            true,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.High,
            }
        );

        return Ok(new { message = nonce });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (string.IsNullOrEmpty(CurrentAddress))
            return Unauthorized("Invalid or missing JWT token");

        return Ok(new { CurrentAddress });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        RemoveCookie(HttpContext);

        return Ok();
    }

    private string CreateToken(Player player)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, player.Id.ToString() },
                { "address", player.WalletAddress.Value },
            },
            Audience = _jwtOptions.Audience,
            Issuer = _jwtOptions.Issuer,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.TokenSecret)),
                SecurityAlgorithms.HmacSha512Signature
            ),
        };

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();

        return handler.CreateToken(descriptor);
    }

    private void SetCookie(string accessToken, HttpContext httpContext)
    {
        httpContext.Response.Cookies.Append(
            _jwtOptions.CookieName,
            accessToken,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(30),
            }
        );
    }

    private void RemoveCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(
            _jwtOptions.CookieName,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UnixEpoch,
            }
        );
    }

    private static string GenerateSecureNonce(int length = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var nonce = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[sizeof(uint)];

        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(buffer);
            uint num = BitConverter.ToUInt32(buffer, 0);
            nonce[i] = chars[(int)(num % (uint)chars.Length)];
        }

        return new string(nonce);
    }

    private static string? ExtractNonceFromMessage(string message)
    {
        var match = Regex.Match(message, @"Nonce:\s*([a-zA-Z0-9]+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
