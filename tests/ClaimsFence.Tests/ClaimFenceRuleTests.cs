using System.Security.Claims;
using Xunit;

namespace ClaimsFence.Tests;

public class ClaimFenceRuleTests
{
    [Fact]
    public void AuthenticatedUser_Succeeds_WhenAuthenticated()
    {
        var result = ClaimFence.Rule()
            .RequiresAuthenticatedUser()
            .Evaluate(TestUsers.Authenticated());

        Assert.True(result.Succeeded);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void AuthenticatedUser_Fails_WhenAnonymous()
    {
        var result = ClaimFence.Rule()
            .RequiresAuthenticatedUser()
            .Evaluate(TestUsers.Anonymous());

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("not_authenticated", failure.Code);
        Assert.Equal("User is not authenticated", failure.Message);
    }

    [Fact]
    public void Role_Succeeds_WhenRolePresent()
    {
        var result = ClaimFence.Rule()
            .RequiresRole("Approver")
            .Evaluate(TestUsers.Authenticated(TestUsers.Role("Approver")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Role_Fails_WhenRoleMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresRole("Approver")
            .Evaluate(TestUsers.Authenticated(TestUsers.Role("Viewer")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_role", failure.Code);
        Assert.Equal("Missing required role: Approver", failure.Message);
    }

    [Fact]
    public void AnyRole_Succeeds_WhenOneRolePresent()
    {
        var result = ClaimFence.Rule()
            .RequiresAnyRole("Admin", "Approver")
            .Evaluate(TestUsers.Authenticated(TestUsers.Role("Approver")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AnyRole_Fails_WhenNoRolePresent()
    {
        var result = ClaimFence.Rule()
            .RequiresAnyRole("Admin", "Approver")
            .Evaluate(TestUsers.Authenticated(TestUsers.Role("Viewer")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_role", failure.Code);
        Assert.Contains("one of [Admin, Approver]", failure.Message);
    }

    [Fact]
    public void RequiredClaim_Succeeds_WhenPresent()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("permission", "Claims.Approve")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "Claims.Approve")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RequiredClaim_Fails_WhenMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("permission", "Claims.Approve")
            .Evaluate(TestUsers.Authenticated());

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_claim", failure.Code);
        Assert.Equal("Missing required claim: permission = Claims.Approve", failure.Message);
    }

    [Fact]
    public void RequiredClaim_Fails_WithMismatchMessage_WhenValueDiffers()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", "tenant-123")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-456")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("claim_mismatch", failure.Code);
        Assert.Equal("Claim mismatch: tenant expected tenant-123 but found tenant-456", failure.Message);
    }

    [Fact]
    public void AnyClaim_Succeeds_WhenOneMatches()
    {
        var result = ClaimFence.Rule()
            .RequiresAnyClaim("permission", "Users.Read", "Users.Write")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "Users.Write")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AnyClaim_Fails_WhenNoneMatch()
    {
        var result = ClaimFence.Rule()
            .RequiresAnyClaim("permission", "Users.Read", "Users.Write")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "Users.Delete")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("claim_mismatch", failure.Code);
    }

    [Fact]
    public void AllClaims_Succeeds_WhenAllPresent()
    {
        var result = ClaimFence.Rule()
            .RequiresAllClaims("scope", "api.read", "api.write")
            .Evaluate(TestUsers.Authenticated(
                TestUsers.Claim("scope", "api.read"),
                TestUsers.Claim("scope", "api.write")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void AllClaims_Fails_WithUsefulMessage_WhenSomeMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresAllClaims("scope", "api.read", "api.write")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("scope", "api.read")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_claims", failure.Code);
        Assert.Equal(
            "Missing required claims: scope must contain all of [api.read, api.write] but is missing [api.write]",
            failure.Message);
    }

    [Fact]
    public void NotClaim_Fails_WhenForbiddenValuePresent()
    {
        var result = ClaimFence.Rule()
            .RequiresNotClaim("account_status", "Suspended")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("account_status", "Suspended")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("forbidden_claim", failure.Code);
        Assert.Equal("Forbidden claim present: account_status = Suspended", failure.Message);
    }

    [Fact]
    public void NotClaim_Succeeds_WhenForbiddenValueAbsent()
    {
        var result = ClaimFence.Rule()
            .RequiresNotClaim("account_status", "Suspended")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("account_status", "Active")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Custom_Predicate_Succeeds()
    {
        var result = ClaimFence.Rule()
            .RequiresCustom("must have name", user => user.Identity?.Name is not null)
            .Evaluate(TestUsers.Authenticated(new Claim(ClaimTypes.Name, "alice")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Custom_Predicate_Fails()
    {
        var result = ClaimFence.Rule()
            .RequiresCustom("must have name", user => user.Identity?.Name is not null)
            .Evaluate(TestUsers.Authenticated());

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("custom_requirement_failed", failure.Code);
        Assert.Equal("must have name", failure.Message);
    }

    [Fact]
    public void Custom_PredicateWithContext_Succeeds()
    {
        var context = new ClaimMatchContext().WithValue("region", "eu");
        var result = ClaimFence.Rule()
            .RequiresCustom("must be in matching region", (user, ctx) =>
                ctx.TryResolve(ClaimValueSource.Header, "region", out var region)
                && user.HasClaim("region", region!))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("region", "eu")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Custom_PredicateWithContext_Fails()
    {
        var context = new ClaimMatchContext().WithValue("region", "us");
        var result = ClaimFence.Rule()
            .RequiresCustom("must be in matching region", (user, ctx) =>
                ctx.TryResolve(ClaimValueSource.Header, "region", out var region)
                && user.HasClaim("region", region!))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("region", "eu")), context);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Evaluate_CollectsMultipleFailures()
    {
        var result = ClaimFence.Rule()
            .RequiresRole("Approver")
            .RequiresClaim("permission", "Claims.Approve")
            .Evaluate(TestUsers.Authenticated());

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public void Evaluate_Throws_WhenUserIsNull()
    {
        var rule = ClaimFence.Rule().RequiresAuthenticatedUser().Build();
        Assert.Throws<ArgumentNullException>(() => rule.Evaluate(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Builder_Throws_ForInvalidRole(string? role)
    {
        Assert.Throws<ArgumentException>(() => ClaimFence.Rule().RequiresRole(role!));
    }

    [Fact]
    public void RequiredClaim_IsCaseSensitive()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("permission", "Claims.Approve")
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "claims.approve")));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Builder_Throws_ForNullClaimType()
    {
        Assert.Throws<ArgumentException>(() => ClaimFence.Rule().RequiresClaim(null!, "value"));
    }
}
