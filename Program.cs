using System.Text;
using MergeCat.Configuration;
using MergeCat.Context;
using MergeCat.Services;
using MergeCat.Services.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var envVariables = builder.Configuration.GetSection(nameof(EnvVariables));

builder.Services.Configure<EnvVariables>(envVariables);

builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<ApiDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention();
});

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = envVariables[nameof(EnvVariables.Issuer)],
            ValidateAudience = true,
            ValidAudience = envVariables[nameof(EnvVariables.Audience)],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(envVariables[nameof(EnvVariables.JwtTokenSecret)])
            ),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (
                    context.Request.Cookies.TryGetValue(
                        envVariables[nameof(EnvVariables.CookieName)],
                        out var token
                    )
                )
                    context.Token = token;

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder
                .WithOrigins(
                    builder.Environment.IsProduction()
                        ? string.Empty // temporary empty
                        : "http://localhost:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

builder.Services.AddSingleton<IUserIdentity, UserIdentity>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseHttpsRedirection();
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
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
