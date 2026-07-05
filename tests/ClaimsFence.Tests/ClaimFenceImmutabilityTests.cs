using System.Collections;
using System.Security.Claims;
using Xunit;

namespace ClaimsFence.Tests;

public class ClaimFenceImmutabilityTests
{
    [Fact]
    public void Build_DoesNotReflectLaterBuilderChanges()
    {
        var builder = ClaimFence.Rule().RequiresRole("Admin");
        var rule = builder.Build();
        builder.RequiresRole("Viewer");

        Assert.Single(rule.Requirements);
    }

    [Fact]
    public void Rule_Requirements_AreReadOnly()
    {
        var rule = ClaimFence.Rule().RequiresRole("Admin").Build();
        var list = (IList)rule.Requirements;

        Assert.Throws<NotSupportedException>(() => list[0] = new TestRequirement());
    }

    [Fact]
    public void FailedResult_DoesNotReflectLaterListChanges()
    {
        var failures = new List<ClaimFenceFailure>
        {
            new("missing_role", "Missing required role: Admin", "Role 'Admin'"),
        };

        var result = ClaimFenceResult.Failed(failures);
        failures.Add(new("missing_claim", "Missing required claim: permission = x", "Claim 'permission'"));

        Assert.Single(result.Failures);
    }

    [Fact]
    public void AnyMatch_DoesNotReflectLaterArrayChanges()
    {
        var values = new[] { "a", "b" };
        var match = ClaimMatch.Any(values);
        values[0] = "changed";

        var result = ClaimFence.Rule()
            .RequiresClaim("permission", match)
            .Evaluate(TestUsers.Authenticated(TestUsers.Claim("permission", "a")));

        Assert.True(result.Succeeded);
    }

    private sealed class TestRequirement : ClaimFenceRequirement
    {
        public override string Description => "Changed requirement";

        public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context) => null;
    }
}
