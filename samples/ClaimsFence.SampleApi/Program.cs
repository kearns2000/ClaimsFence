using System.Security.Claims;
using System.Text.Encodings.Web;
using ClaimsFence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Development-only header based authentication so the sample can be exercised without a real
// identity provider. Send these headers on requests:
//   X-User    : any non-empty value marks the request as authenticated (used as the name)
//   X-Roles   : comma separated roles, e.g. "Approver,Admin"
//   X-Perms   : comma separated "permission" claims, e.g. "Claims.Approve"
//   X-Tenant  : the "tenant" claim value, e.g. "tenant-123"
builder.Services
    .AddAuthentication(DevAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, null);

builder.Services.AddAuthorization(options =>
{
    // Policy based usage: the classic AddPolicy + RequireClaimFence style.
    options.AddPolicy("CanApproveClaim", policy =>
        policy.RequireClaimFence(fence =>
            fence.RequiresAuthenticatedUser()
                 .RequiresRole("Approver")
                 .RequiresClaim("permission", "Claims.Approve")
                 .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))));
});

// Registers the ClaimsFence authorisation handler.
builder.Services.AddClaimFence();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Public endpoint: no authorisation at all.
app.MapGet("/public", () => "Anyone can see this.");

// Endpoint based usage: a simple role check applied directly to the endpoint.
app.MapGet("/admin", () => "Welcome, admin!")
   .RequireClaimFence(fence =>
        fence.RequiresAuthenticatedUser()
             .RequiresRole("Admin"));

// Policy based usage: apply the named "CanApproveClaim" policy. The tenant claim must match
// the {tenantId} route value.
app.MapPost("/tenants/{tenantId}/claims/{claimId}/approve",
        (string tenantId, string claimId) => Results.Ok($"Approved claim {claimId} for {tenantId}."))
   .RequireAuthorization("CanApproveClaim");

app.Run();

/// <summary>
/// A development-only authentication handler that builds a user from request headers so the
/// sample can be demonstrated without a real identity provider. Do not use in production.
/// </summary>
internal sealed class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Dev";

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-User", out var user) || string.IsNullOrWhiteSpace(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, user.ToString()) };

        AddValues(Request.Headers["X-Roles"].ToString(), value => claims.Add(new Claim(ClaimTypes.Role, value)));
        AddValues(Request.Headers["X-Perms"].ToString(), value => claims.Add(new Claim("permission", value)));

        var tenant = Request.Headers["X-Tenant"].ToString();
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            claims.Add(new Claim("tenant", tenant));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddValues(string raw, Action<string> add)
    {
        foreach (var value in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            add(value);
        }
    }
}

/// <summary>
/// Exposed so the integration test project can host the sample with a test server.
/// </summary>
public partial class Program;
