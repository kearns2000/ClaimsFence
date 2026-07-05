using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ClaimsFence.Tests;

public class AspNetCoreIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AspNetCoreIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicEndpoint_IsAccessible_WithoutAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/public");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointRule_Succeeds_ForAuthorizedUser()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin");
        request.Headers.Add("X-User", "alice");
        request.Headers.Add("X-Roles", "Admin");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointRule_Challenges_AnonymousUser()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointRule_Forbids_AuthenticatedUserWithoutRole()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin");
        request.Headers.Add("X-User", "bob");
        request.Headers.Add("X-Roles", "Viewer");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PolicyRule_Succeeds_WhenTenantRouteValueMatchesClaim()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/tenants/tenant-123/claims/c1/approve");
        request.Headers.Add("X-User", "carol");
        request.Headers.Add("X-Roles", "Approver");
        request.Headers.Add("X-Perms", "Claims.Approve");
        request.Headers.Add("X-Tenant", "tenant-123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PolicyRule_Forbids_WhenTenantRouteValueDoesNotMatchClaim()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/tenants/tenant-123/claims/c1/approve");
        request.Headers.Add("X-User", "carol");
        request.Headers.Add("X-Roles", "Approver");
        request.Headers.Add("X-Perms", "Claims.Approve");
        request.Headers.Add("X-Tenant", "tenant-999");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FailureDetails_AreNotLeaked_InResponseBody_ByDefault()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin");
        request.Headers.Add("X-User", "bob");
        request.Headers.Add("X-Roles", "Viewer");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("Missing required role", body, StringComparison.OrdinalIgnoreCase);
    }
}
