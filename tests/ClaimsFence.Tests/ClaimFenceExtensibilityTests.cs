using System.Security.Claims;
using Xunit;

namespace ClaimsFence.Tests;

public class ClaimFenceExtensibilityTests
{
    [Fact]
    public void Requires_AddsCustomRequirement()
    {
        var rule = ClaimFence.Rule()
            .Requires(new AlwaysPassRequirement())
            .Build();

        var result = rule.Evaluate(TestUsers.Anonymous());

        Assert.True(result.Succeeded);
    }

    private sealed class AlwaysPassRequirement : ClaimFenceRequirement
    {
        public override string Description => "Always passes";

        public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context) => null;
    }
}
