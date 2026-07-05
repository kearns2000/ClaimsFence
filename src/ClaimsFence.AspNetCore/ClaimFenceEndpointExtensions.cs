using ClaimsFence.AspNetCore;
using Microsoft.AspNetCore.Authorization;

// Placed in the ASP.NET Core builder namespace so the extension is discoverable alongside
// RequireAuthorization and the other endpoint convention builder extensions.
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for applying a ClaimsFence rule directly to an endpoint.
/// </summary>
public static class ClaimFenceEndpointExtensions
{
    /// <summary>
    /// Applies a ClaimsFence rule to the endpoint(s) produced by the builder. This adds an
    /// authorisation policy to the endpoint metadata, so the endpoint is authorised by the
    /// standard ASP.NET Core authorisation middleware.
    /// <para>
    /// Requires <c>AddClaimFence()</c> to have been called during service registration and
    /// <c>UseAuthorization()</c> to be present in the middleware pipeline.
    /// </para>
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="configure">Configures the fence rule.</param>
    /// <param name="configureOptions">Optionally configures diagnostics options for this rule.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static TBuilder RequireClaimFence<TBuilder>(
        this TBuilder builder,
        Action<ClaimsFence.ClaimFenceBuilder> configure,
        Action<ClaimsFence.ClaimFenceOptions>? configureOptions = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var fence = new ClaimsFence.ClaimFenceBuilder();
        configure(fence);

        var options = new ClaimsFence.ClaimFenceOptions();
        configureOptions?.Invoke(options);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new ClaimFenceAuthorizationRequirement(fence.Build(), options))
            .Build();

        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(policy));
        return builder;
    }
}
