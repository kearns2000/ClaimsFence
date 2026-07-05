using System.Security.Claims;

namespace ClaimsFence.Tests;

/// <summary>
/// Helpers for building <see cref="ClaimsPrincipal"/> instances in tests.
/// </summary>
internal static class TestUsers
{
    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static ClaimsPrincipal Authenticated(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    public static Claim Role(string role) => new(ClaimTypes.Role, role);

    public static Claim Claim(string type, string value) => new(type, value);
}
