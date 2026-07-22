using System.Text;
using System.Text.Json;
using MergeCat.Context;
using MergeCat.Endpoints;
using MergeCat.Options;
using MergeCat.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Web3;

var builder = WebApplication.CreateBuilder(args);
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var jwtOptions = builder.Configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>()!;

builder.Services.AddOptions<JwtOptions>().BindConfiguration(nameof(JwtOptions));
builder.Services.AddOptions<GameOptions>().BindConfiguration(nameof(GameOptions));
builder.Services.AddOptions<BlockchainOptions>().BindConfiguration(nameof(BlockchainOptions));

builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention();
});

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.TokenSecret)
            ),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(jwtOptions.CookieName, out var token))
                    context.Token = token;

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder
                .WithOrigins(
                    builder.Environment.IsProduction()
                        ?
                        [
                            "http://localhost:3000", // temporary same as dev
                            "https://lilac-placate-handgun.ngrok-free.dev",
                        ]
                        : ["http://localhost:3000", "https://lilac-placate-handgun.ngrok-free.dev"]
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

builder.Services.AddHostedService<OnChainPurchaseIndexer>();
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<BlockchainOptions>>().Value;
    return new Web3(options.RpcUrl);
});
builder.Services.AddSingleton<PurchaseNotificationHub>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddMemoryCache();

var app = builder.Build();
app.UseRouting();
app.UseCors(myAllowSpecificOrigins);
app.UseCookiePolicy(
    new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.None,
        HttpOnly = HttpOnlyPolicy.Always,
        Secure = CookieSecurePolicy.Always,
    }
);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapNotificationEndpoints();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
