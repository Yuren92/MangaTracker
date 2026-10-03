using MangaTracker.Api.Auth;
using MangaTracker.Api.Middleware;
using MangaTracker.Application;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Infrastructure;
using MangaTracker.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MangaTracker.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Token validation reads the same validated JwtOptions used to issue tokens,
// so issuing and validating can never drift apart.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Only the proxies listed in configuration are trusted to report the client IP through
// X-Forwarded-For. Trusting the header from anyone would let a caller pick a new
// "IP" on every request and bypass the per-IP limits on anonymous endpoints.
var trustedProxies = builder.Configuration
    .GetSection("ForwardedHeaders:KnownProxies")
    .Get<string[]>() ?? [];

var trustedNetworks = builder.Configuration
    .GetSection("ForwardedHeaders:KnownNetworks")
    .Get<string[]>() ?? [];

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Empty KnownProxies and KnownIPNetworks lists mean "trust every proxy" in ASP.NET,
    // so with nothing configured the headers are ignored altogether.
    if (trustedProxies.Length == 0 && trustedNetworks.Length == 0)
    {
        options.ForwardedHeaders = ForwardedHeaders.None;
        return;
    }

    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Replace the default (loopback) with exactly the configured proxies.
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    foreach (var proxy in trustedProxies)
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }

    foreach (var network in trustedNetworks)
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

builder.Services.AddRateLimiter(options =>
{
    AddFixedWindowPolicy(options, RateLimitPolicies.AuthSensitive, permitLimit: 5);
    AddFixedWindowPolicy(options, RateLimitPolicies.ExternalApi, permitLimit: 30);
    AddFixedWindowPolicy(options, RateLimitPolicies.ComicVineImport, permitLimit: 5);

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Title = "Too many requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Too many requests. Please wait a moment before trying again."
        };

        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);
    };
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MangaTracker.Infrastructure.Persistence.MangaTrackerDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Must run first so every later component sees the real client IP and scheme.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("frontend");

// Authentication runs before rate limiting so authenticated endpoints can be
// limited per user instead of per IP.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

static void AddFixedWindowPolicy(RateLimiterOptions options, string policyName, int permitLimit)
{
    options.AddPolicy(policyName, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKey.For(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
}
