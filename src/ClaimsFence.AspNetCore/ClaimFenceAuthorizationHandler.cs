using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClaimsFence.AspNetCore;

/// <summary>
/// The ASP.NET Core authorisation handler for <see cref="ClaimFenceAuthorizationRequirement"/>.
/// It builds a <see cref="ClaimMatchContext"/> from the current
/// <see cref="HttpContext"/> (route values, headers, and query string) and evaluates the rule.
/// </summary>
public sealed class ClaimFenceAuthorizationHandler : AuthorizationHandler<ClaimFenceAuthorizationRequirement>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initialises a new instance of the <see cref="ClaimFenceAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// An optional <see cref="IHttpContextAccessor"/> used as a fallback when the current
    /// <see cref="HttpContext"/> cannot be resolved from the authorisation resource.
    /// </param>
    public ClaimFenceAuthorizationHandler(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimFenceAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var matchContext = BuildContext(ResolveHttpContext(context));
        var result = requirement.Rule.Evaluate(context.User, matchContext);

        if (result.Succeeded)
        {
            context.Succeed(requirement);
        }
        else if (requirement.Options.IncludeFailureMessagesInAuthorizationFailure)
        {
            var message = string.Join("; ", result.Failures.Select(f => f.Message));
            context.Fail(new AuthorizationFailureReason(this, message));
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }

    private HttpContext? ResolveHttpContext(AuthorizationHandlerContext context)
    {
        // Minimal APIs and endpoint routing expose the HttpContext directly; MVC exposes an
        // AuthorizationFilterContext. Fall back to the accessor for any other host.
        return context.Resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => _httpContextAccessor?.HttpContext,
        };
    }

    private static ClaimMatchContext BuildContext(HttpContext? httpContext)
    {
        var matchContext = new ClaimMatchContext();
        if (httpContext is null)
        {
            return matchContext;
        }

        var request = httpContext.Request;

        foreach (var routeValue in request.RouteValues)
        {
            matchContext.WithRouteValue(routeValue.Key, routeValue.Value?.ToString());
        }

        foreach (var header in request.Headers)
        {
            matchContext.WithHeaderValue(header.Key, header.Value.ToString());
        }

        foreach (var query in request.Query)
        {
            matchContext.WithQueryValue(query.Key, query.Value.ToString());
        }

        return matchContext;
    }
}
