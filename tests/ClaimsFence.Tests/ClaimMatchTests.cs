using Xunit;

namespace ClaimsFence.Tests;

public class ClaimMatchTests
{
    [Fact]
    public void Value_Match_Succeeds()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.Value("tenant-123"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Predicate_Match_Succeeds()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.Predicate("must start with tenant-", v => v.StartsWith("tenant-", StringComparison.Ordinal)))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-999")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Predicate_Match_Fails_WithDescription()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.Predicate("must start with tenant-", v => v.StartsWith("tenant-", StringComparison.Ordinal)))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "org-1")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("claim_predicate_failed", failure.Code);
        Assert.Contains("must start with tenant-", failure.Message);
    }

    [Fact]
    public void Any_Match_Succeeds()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("permission", ClaimMatch.Any("Claims.Read", "Claims.Write"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "Claims.Write")));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RouteValue_Match_Succeeds_UsingContext()
    {
        var context = new ClaimMatchContext().WithValue("tenantId", "tenant-123");
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RouteValue_Match_UsesRouteSpecificValue()
    {
        var context = new ClaimMatchContext().WithRouteValue("tenantId", "tenant-123");
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RouteValue_Match_Fails_WithUsefulMessage_WhenValueMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("unresolved_match_value", failure.Code);
        Assert.Contains("route value 'tenantId'", failure.Message);
    }

    [Fact]
    public void RouteValue_Match_Fails_WhenClaimDiffersFromRoute()
    {
        var context = new ClaimMatchContext().WithRouteValue("tenantId", "tenant-123");
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-999")), context);

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("claim_mismatch", failure.Code);
    }

    [Fact]
    public void Header_Match_Succeeds_UsingContext()
    {
        var context = new ClaimMatchContext().WithHeaderValue("X-Tenant-Id", "tenant-123");
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.Header("X-Tenant-Id"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void QueryString_Match_Succeeds_UsingContext()
    {
        var context = new ClaimMatchContext().WithQueryValue("tenant", "tenant-123");
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.QueryString("tenant"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Any_Match_Throws_WhenNoValues()
    {
        Assert.Throws<ArgumentException>(() => ClaimMatch.Any());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Any_Match_Throws_WhenValuesContainWhitespace(string invalidValue)
    {
        Assert.Throws<ArgumentException>(() => ClaimMatch.Any(invalidValue, "valid"));
    }

    [Fact]
    public void Any_Match_Fails_WithMissingClaimMessage_WhenClaimAbsent()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("permission", ClaimMatch.Any("Claims.Read", "Claims.Write"))
            .Evaluate(TestUsers.Authenticated());

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_claim", failure.Code);
    }

    [Fact]
    public void RouteValue_Match_FallsBack_ToGenericValue()
    {
        var context = new ClaimMatchContext()
            .WithRouteValue("tenantId", null)
            .WithValue("tenantId", "tenant-123");

        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.RouteValue("tenantId"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")), context);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Header_Match_Fails_WithUsefulMessage_WhenValueMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.Header("X-Tenant-Id"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("unresolved_match_value", failure.Code);
        Assert.Contains("header 'X-Tenant-Id'", failure.Message);
    }

    [Fact]
    public void QueryString_Match_Fails_WithUsefulMessage_WhenValueMissing()
    {
        var result = ClaimFence.Rule()
            .RequiresClaim("tenant", ClaimMatch.QueryString("tenant"))
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("tenant", "tenant-123")));

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("unresolved_match_value", failure.Code);
        Assert.Contains("query string value 'tenant'", failure.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RouteValue_Match_Throws_ForWhitespaceKey(string key)
    {
        Assert.Throws<ArgumentException>(() => ClaimMatch.RouteValue(key));
    }

    [Fact]
    public void Predicate_Match_Throws_WhenPredicateNull()
    {
        Assert.Throws<ArgumentNullException>(() => ClaimMatch.Predicate("desc", null!));
    }
}
