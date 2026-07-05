using ClaimsFence.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Placed in the ASP.NET Core builder namespace so the extensions are discoverable
// alongside the other authorisation configuration APIs.
namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Extension methods for registering ClaimsFence with ASP.NET Core authorisation.
/// </summary>
public static class ClaimFenceAuthorizationExtensions
{
    /// <summary>
    /// Registers the ClaimsFence authorisation handler so that policies and endpoints using
    /// ClaimsFence can be evaluated. Call this once during service registration. Calling it
    /// more than once is safe and does not register the handler twice.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddClaimFence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ClaimFenceAuthorizationHandler>());
        return services;
    }

    /// <summary>
    /// Adds a ClaimsFence rule as a requirement on an authorisation policy.
    /// </summary>
    /// <param name="builder">The policy builder.</param>
    /// <param name="configure">Configures the fence rule.</param>
    /// <param name="configureOptions">Optionally configures diagnostics options for this rule.</param>
    /// <returns>The same policy builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static AuthorizationPolicyBuilder RequireClaimFence(
        this AuthorizationPolicyBuilder builder,
        Action<ClaimsFence.ClaimFenceBuilder> configure,
        Action<ClaimsFence.ClaimFenceOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var fence = new ClaimsFence.ClaimFenceBuilder();
        configure(fence);

        var options = new ClaimsFence.ClaimFenceOptions();
        configureOptions?.Invoke(options);

        builder.AddRequirements(new ClaimFenceAuthorizationRequirement(fence.Build(), options));
        return builder;
    }
}
