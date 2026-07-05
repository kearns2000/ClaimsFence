using Xunit;

namespace ClaimsFence.Tests;

public class ClaimFenceFailureTests
{
    [Fact]
    public void Failure_ExposesCodeMessageAndRequirement()
    {
        var result = ClaimFence.Rule()
            .RequiresRole("Approver")
            .Evaluate(TestUsers.Authenticated());

        var failure = Assert.Single(result.Failures);
        Assert.Equal("missing_role", failure.Code);
        Assert.Equal("Missing required role: Approver", failure.Message);
        Assert.Equal("Role 'Approver'", failure.Requirement);
    }

    [Fact]
    public void Failure_ToString_ReturnsMessage()
    {
        var failure = new ClaimFenceFailure("code", "the message", "the requirement");
        Assert.Equal("the message", failure.ToString());
    }

    [Fact]
    public void Failure_Throws_WhenArgumentsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaimFenceFailure(null!, "m", "r"));
        Assert.Throws<ArgumentNullException>(() => new ClaimFenceFailure("c", null!, "r"));
        Assert.Throws<ArgumentNullException>(() => new ClaimFenceFailure("c", "m", null!));
    }

    [Fact]
    public void Result_Success_HasNoFailures()
    {
        Assert.True(ClaimFenceResult.Success.Succeeded);
        Assert.Empty(ClaimFenceResult.Success.Failures);
    }

    [Fact]
    public void Result_Failed_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => ClaimFenceResult.Failed(Array.Empty<ClaimFenceFailure>()));
    }

    [Fact]
    public void Rule_ExposesRequirementsInOrder()
    {
        var rule = ClaimFence.Rule()
            .RequiresAuthenticatedUser()
            .RequiresRole("Admin")
            .Build();

        Assert.Equal(2, rule.Requirements.Count);
        Assert.Equal("Authenticated user", rule.Requirements[0].Description);
        Assert.Equal("Role 'Admin'", rule.Requirements[1].Description);
    }
}
