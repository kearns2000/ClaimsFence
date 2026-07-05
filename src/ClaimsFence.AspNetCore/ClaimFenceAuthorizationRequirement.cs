using Microsoft.AspNetCore.Authorization;

namespace ClaimsFence.AspNetCore;

/// <summary>
/// An ASP.NET Core <see cref="IAuthorizationRequirement"/> that wraps a
/// <see cref="ClaimFenceRule"/> so it can participate in the authorisation pipeline.
/// </summary>
public sealed class ClaimFenceAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ClaimFenceAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="options">Options controlling failure detail exposure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ClaimFenceAuthorizationRequirement(ClaimFenceRule rule, ClaimFenceOptions options)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the rule evaluated by this requirement.
    /// </summary>
    public ClaimFenceRule Rule { get; }

    /// <summary>
    /// Gets the options that control how failures are surfaced.
    /// </summary>
    public ClaimFenceOptions Options { get; }
}
