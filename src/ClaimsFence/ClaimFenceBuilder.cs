using System.Security.Claims;

namespace ClaimsFence;

/// <summary>
/// A fluent builder for composing a <see cref="ClaimFenceRule"/>. Each method adds a
/// requirement and returns the same builder so calls can be chained.
/// </summary>
public sealed class ClaimFenceBuilder
{
    private readonly List<ClaimFenceRequirement> _requirements = new();

    /// <summary>
    /// Adds a custom <see cref="ClaimFenceRequirement"/> to the rule.
    /// </summary>
    /// <param name="requirement">The requirement to add.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requirement"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder Requires(ClaimFenceRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        _requirements.Add(requirement);
        return this;
    }

    /// <summary>
    /// Requires the user to be authenticated.
    /// </summary>
    /// <returns>The same builder for chaining.</returns>
    public ClaimFenceBuilder RequiresAuthenticatedUser()
    {
        _requirements.Add(new AuthenticatedUserRequirement());
        return this;
    }

    /// <summary>
    /// Requires the user to be in the given role.
    /// </summary>
    /// <param name="role">The required role.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="role"/> is null or empty.</exception>
    public ClaimFenceBuilder RequiresRole(string role)
    {
        _requirements.Add(new RoleRequirement(ClaimFenceGuard.RequireText(role, nameof(role))));
        return this;
    }

    /// <summary>
    /// Requires the user to be in at least one of the given roles.
    /// </summary>
    /// <param name="roles">The candidate roles.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="roles"/> is null, empty, or contains a null or empty role.</exception>
    public ClaimFenceBuilder RequiresAnyRole(params string[] roles)
    {
        _requirements.Add(new AnyRoleRequirement(ClaimFenceGuard.RequireTextArray(roles, nameof(roles))));
        return this;
    }

    /// <summary>
    /// Requires the user to have a claim of the given type equal to the expected value.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="expectedValue">The expected claim value.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="claimType"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedValue"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder RequiresClaim(string claimType, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);
        return RequiresClaim(claimType, ClaimMatch.Value(expectedValue));
    }

    /// <summary>
    /// Requires the user to have a claim of the given type matching the supplied
    /// <see cref="ClaimMatch"/>.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="match">The match to apply to the claim value.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="claimType"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="match"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder RequiresClaim(string claimType, ClaimMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);
        _requirements.Add(new ClaimMatchRequirement(ClaimFenceGuard.RequireText(claimType, nameof(claimType)), match));
        return this;
    }

    /// <summary>
    /// Requires the user to have a claim of the given type whose value is any of the supplied values.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="values">The allowed values.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="claimType"/> is null or empty, or <paramref name="values"/> is null or empty.</exception>
    public ClaimFenceBuilder RequiresAnyClaim(string claimType, params string[] values)
    {
        _requirements.Add(new ClaimMatchRequirement(ClaimFenceGuard.RequireText(claimType, nameof(claimType)), ClaimMatch.Any(values)));
        return this;
    }

    /// <summary>
    /// Requires the user to have claims of the given type covering every supplied value.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="values">The values that must all be present.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="claimType"/> is null or empty, or <paramref name="values"/> is null, empty, or contains a null or empty value.</exception>
    public ClaimFenceBuilder RequiresAllClaims(string claimType, params string[] values)
    {
        _requirements.Add(new AllClaimsRequirement(ClaimFenceGuard.RequireText(claimType, nameof(claimType)), ClaimFenceGuard.RequireTextArray(values, nameof(values))));
        return this;
    }

    /// <summary>
    /// Requires that the user does not have a claim of the given type with the forbidden value.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="forbiddenValue">The value that must not be present.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="claimType"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="forbiddenValue"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder RequiresNotClaim(string claimType, string forbiddenValue)
    {
        ArgumentNullException.ThrowIfNull(forbiddenValue);
        _requirements.Add(new ForbiddenClaimRequirement(ClaimFenceGuard.RequireText(claimType, nameof(claimType)), forbiddenValue));
        return this;
    }

    /// <summary>
    /// Requires a custom condition described by <paramref name="description"/>.
    /// </summary>
    /// <param name="description">A human readable description used in diagnostics.</param>
    /// <param name="predicate">The condition the user must satisfy.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="description"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder RequiresCustom(string description, Func<ClaimsPrincipal, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return RequiresCustom(description, (user, _) => predicate(user));
    }

    /// <summary>
    /// Requires a custom condition described by <paramref name="description"/> that also has
    /// access to the <see cref="ClaimMatchContext"/>.
    /// </summary>
    /// <param name="description">A human readable description used in diagnostics.</param>
    /// <param name="predicate">The condition the user must satisfy.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="description"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public ClaimFenceBuilder RequiresCustom(string description, Func<ClaimsPrincipal, ClaimMatchContext, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _requirements.Add(new CustomRequirement(ClaimFenceGuard.RequireText(description, nameof(description)), predicate));
        return this;
    }

    /// <summary>
    /// Builds an immutable <see cref="ClaimFenceRule"/> from the configured requirements.
    /// </summary>
    /// <returns>The built rule.</returns>
    public ClaimFenceRule Build() => new(_requirements.ToArray());

    /// <summary>
    /// Convenience method that builds the rule and evaluates it against a user.
    /// </summary>
    /// <param name="user">The user to evaluate.</param>
    /// <returns>The evaluation result.</returns>
    public ClaimFenceResult Evaluate(ClaimsPrincipal user) => Build().Evaluate(user);

    /// <summary>
    /// Convenience method that builds the rule and evaluates it against a user with context.
    /// </summary>
    /// <param name="user">The user to evaluate.</param>
    /// <param name="context">External values available to contextual matches.</param>
    /// <returns>The evaluation result.</returns>
    public ClaimFenceResult Evaluate(ClaimsPrincipal user, ClaimMatchContext context) => Build().Evaluate(user, context);
}
