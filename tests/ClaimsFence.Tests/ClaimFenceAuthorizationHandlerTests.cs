using ClaimsFence.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ClaimsFence.Tests;

public class ClaimFenceAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_IncludesFailureReasons_WhenOptedIn()
    {
        var requirement = new ClaimFenceAuthorizationRequirement(
            ClaimFence.Rule().RequiresRole("Admin").Build(),
            new ClaimFenceOptions { IncludeFailureMessagesInAuthorizationFailure = true });

        var handler = new ClaimFenceAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            [requirement],
            TestUsers.Authenticated(TestUsers.Role("Viewer")),
            new DefaultHttpContext());

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        var reason = Assert.Single(context.FailureReasons);
        Assert.Contains("Missing required role: Admin", reason.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_DoesNotIncludeFailureReasons_ByDefault()
    {
        var requirement = new ClaimFenceAuthorizationRequirement(
            ClaimFence.Rule().RequiresRole("Admin").Build(),
            new ClaimFenceOptions());

        var handler = new ClaimFenceAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            [requirement],
            TestUsers.Authenticated(TestUsers.Role("Viewer")),
            new DefaultHttpContext());

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Empty(context.FailureReasons);
    }

    [Fact]
    public async Task Handler_ResolvesRouteValues_FromHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["tenantId"] = "tenant-123";

        var requirement = new ClaimFenceAuthorizationRequirement(
            ClaimFence.Rule()
                .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
                .Build(),
            new ClaimFenceOptions());

        var handler = new ClaimFenceAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            [requirement],
            TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")),
            httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
