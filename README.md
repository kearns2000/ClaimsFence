<p align="center">
  <img src="https://raw.githubusercontent.com/kearns2000/ClaimsFence/main/assets/icon.png" alt="ClaimsFence" width="140" height="140" />
</p>

# ClaimsFence

<p align="center">
  <a href="https://github.com/kearns2000/ClaimsFence/actions/workflows/ci.yml"><img src="https://github.com/kearns2000/ClaimsFence/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <a href="https://www.nuget.org/packages/ClaimsFence"><img src="https://img.shields.io/nuget/v/ClaimsFence.svg?logo=nuget&label=NuGet" alt="NuGet version" /></a>
  <a href="https://www.nuget.org/packages/ClaimsFence.AspNetCore"><img src="https://img.shields.io/nuget/v/ClaimsFence.AspNetCore.svg?logo=nuget&label=AspNetCore" alt="NuGet AspNetCore version" /></a>
  <a href="https://www.nuget.org/packages/ClaimsFence"><img src="https://img.shields.io/nuget/dt/ClaimsFence.svg?logo=nuget&label=Downloads" alt="NuGet downloads" /></a>
  <a href="https://github.com/kearns2000/ClaimsFence/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License: MIT" /></a>
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10.0" />
</p>

Lightweight, testable claims authorisation rules for .NET.

ClaimsFence is a small rule builder and evaluator for claims-based authorisation. Think
FluentValidation, but for claims. It lets you express "who is allowed to do this" as a
reusable, testable object, and it tells you *exactly* why a check failed.

## What it is

ClaimsFence turns scattered authorisation checks like this:

```csharp
if (!user.HasClaim("permission", "Claims.Approve") ||
    !user.HasClaim("tenant", tenantId) ||
    !user.IsInRole("Approver"))
{
    return Results.Forbid();
}
```

into reusable, testable rules like this:

```csharp
var rule = ClaimFence.Rule()
    .RequiresAuthenticatedUser()
    .RequiresRole("Approver")
    .RequiresClaim("permission", "Claims.Approve")
    .RequiresClaim("tenant", ClaimMatch.Value(tenantId));

ClaimFenceResult result = rule.Evaluate(user);
```

## Why it exists

Authorisation logic tends to sprawl. The same claim checks get copied across controllers,
handlers, and background jobs, each slightly different, and none of them easy to unit test.
When a request is denied, you often get a bare `403` with no explanation.

ClaimsFence gives you three things:

- **Reusable rules** - build a rule once, evaluate it anywhere.
- **Testable rules** - a rule is a plain object you can evaluate in a unit test.
- **Clear diagnostics** - every failure has a code, a message, and the requirement that
  produced it, so you can see precisely why a check failed.

It is **not** a replacement for ASP.NET Core authorisation. It plugs into it.

## Installation

The core package has no ASP.NET Core dependency and can be used in background workers,
console apps, and tests:

```bash
dotnet add package ClaimsFence
```

For ASP.NET Core policy and Minimal API integration, also install:

```bash
dotnet add package ClaimsFence.AspNetCore
```

Both packages target `net10.0`.

Claim and role value matching is **case-sensitive** (ordinal comparison). Role checks use
`ClaimsPrincipal.IsInRole`, which follows your identity configuration.

## Basic usage

```csharp
using ClaimsFence;

var rule = ClaimFence.Rule()
    .RequiresAuthenticatedUser()
    .RequiresRole("Admin")
    .RequiresClaim("permission", "Users.Read")
    .RequiresAnyClaim("permission", "Users.Read", "Users.Write")
    .RequiresAllClaims("scope", "api.read", "api.write")
    .RequiresNotClaim("account_status", "Suspended")
    .RequiresClaim("tenant", ClaimMatch.Value("tenant-123"));

ClaimFenceResult result = rule.Evaluate(user);

if (!result.Succeeded)
{
    foreach (var failure in result.Failures)
    {
        Console.WriteLine(failure.Message);
    }
}
```

### Builder methods

| Method | Meaning |
| --- | --- |
| `RequiresAuthenticatedUser()` | The user must be authenticated. |
| `RequiresRole(role)` | The user must be in the role. |
| `RequiresAnyRole(params roles)` | The user must be in at least one role. |
| `RequiresClaim(type, value)` | The user must have a claim equal to `value`. |
| `RequiresClaim(type, ClaimMatch)` | The user must have a claim matching the `ClaimMatch`. |
| `RequiresAnyClaim(type, params values)` | The claim value must be one of `values`. |
| `RequiresAllClaims(type, params values)` | The user must have claims covering every value. |
| `RequiresNotClaim(type, forbiddenValue)` | The user must not have that claim value. |
| `RequiresCustom(description, predicate)` | An arbitrary condition over the user (`Func<ClaimsPrincipal, bool>`). |
| `RequiresCustom(description, predicate)` | An arbitrary condition over the user and `ClaimMatchContext`. |

### Matching

`ClaimMatch` describes how a claim value should be matched:

```csharp
ClaimMatch.Value("tenant-123")
ClaimMatch.Any("Claims.Read", "Claims.Write")
ClaimMatch.Predicate("must start with tenant-", value => value.StartsWith("tenant-"))
ClaimMatch.RouteValue("tenantId")
ClaimMatch.Header("X-Tenant-Id")
ClaimMatch.QueryString("tenant")
```

`Value`, `Any`, and `Predicate` work everywhere. `RouteValue`, `Header`, and `QueryString`
are resolved from the current `HttpContext` inside ASP.NET Core, or from a
`ClaimMatchContext` outside it (see below).

## ASP.NET Core policy usage

Install `ClaimsFence.AspNetCore`, register the handler once, then use `RequireClaimFence`
inside a policy:

```csharp
using Microsoft.AspNetCore.Authorization; // for RequireClaimFence / AddClaimFence

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanApproveClaim", policy =>
    {
        policy.RequireClaimFence(fence =>
            fence.RequiresAuthenticatedUser()
                 .RequiresRole("Approver")
                 .RequiresClaim("permission", "Claims.Approve")
                 .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId")));
    });
});

builder.Services.AddClaimFence(); // registers the authorisation handler

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

Apply the policy the usual way:

```csharp
app.MapPost("/tenants/{tenantId}/claims/{claimId}/approve", ApproveClaim)
   .RequireAuthorization("CanApproveClaim");
```

## Minimal API usage

You can also attach a rule directly to an endpoint without naming a policy:

```csharp
using Microsoft.AspNetCore.Builder; // for RequireClaimFence on endpoints

app.MapPost("/tenants/{tenantId}/claims/{claimId}/approve", ApproveClaim)
   .RequireClaimFence(fence =>
       fence.RequiresAuthenticatedUser()
            .RequiresClaim("permission", "Claims.Approve")
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId")));
```

This adds an authorisation policy to the endpoint metadata, so it is evaluated by the
standard authorisation middleware. It still requires `AddClaimFence()` and
`UseAuthorization()`.

## Non-HTTP / background worker usage

There is no `HttpContext` in a queue consumer, background service, or test. Supply route,
header, and query values yourself with a `ClaimMatchContext`:

```csharp
var context = new ClaimMatchContext()
    .WithValue("tenantId", "tenant-123");

var rule = ClaimFence.Rule()
    .RequiresAuthenticatedUser()
    .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"));

ClaimFenceResult result = rule.Evaluate(user, context);
```

If a contextual match cannot be resolved, you get a clear failure rather than a silent
denial:

```text
Cannot resolve route value 'tenantId' for claim 'tenant'. Supply it through a ClaimMatchContext or evaluate inside ASP.NET Core.
```

## Diagnostics

Diagnostics are the point of ClaimsFence. Every failure carries a stable `Code`, a human
readable `Message`, and the `Requirement` that produced it:

```csharp
var result = rule.Evaluate(user);
foreach (var failure in result.Failures)
{
    Console.WriteLine($"[{failure.Code}] {failure.Message}");
}
```

Example messages:

```text
Missing required claim: permission = Claims.Approve
Missing required role: Approver
User is not authenticated
Forbidden claim present: account_status = Suspended
Claim mismatch: tenant expected tenant-123 but found tenant-456
```

### Not leaking details over HTTP

In ASP.NET Core, detailed failure messages are **not** attached to the authorisation
failure by default, so they will not leak to callers. Opt in for development or logging:

```csharp
policy.RequireClaimFence(
    fence => fence.RequiresRole("Approver"),
    options => options.IncludeFailureMessagesInAuthorizationFailure = true);
```

When enabled, messages are attached as `AuthorizationFailureReason` entries, where your
logging or a custom `IAuthorizationMiddlewareResultHandler` can read them. They are still
not written to the response body by ClaimsFence.

## What this package deliberately does not do

ClaimsFence is a rule builder and evaluator. It is intentionally small. It does **not**
provide:

- user management
- role or permission storage
- an admin UI or policy management UI
- database-backed permissions
- identity provider integration
- OAuth / OIDC server features

It does not replace ASP.NET Core authorisation; it makes your claim rules reusable,
testable, and easier to debug.

## Sample

See `samples/ClaimsFence.SampleApi` for a runnable Minimal API that demonstrates a public
endpoint, a role check, a permission claim check, and tenant route-value matching using a
development-only header-based authentication scheme.

## License

MIT. See [LICENSE](LICENSE).
