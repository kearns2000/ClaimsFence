using System.Security.Claims;

namespace ClaimsFence;

/// <summary>
/// A single requirement that a user must satisfy for a <see cref="ClaimFenceRule"/> to pass.
/// </summary>
public abstract class ClaimFenceRequirement
{
    /// <summary>
    /// Gets a human readable description of the requirement, used in diagnostics and failures.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Checks the requirement against a user.
    /// </summary>
    /// <param name="user">The user being evaluated.</param>
    /// <param name="context">External values available during evaluation.</param>
    /// <returns>
    /// <see langword="null"/> if the requirement is satisfied; otherwise a
    /// <see cref="ClaimFenceFailure"/> describing why it failed.
    /// </returns>
    public abstract ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context);

    private protected ClaimFenceFailure Failure(string code, string message) => new(code, message, Description);
}

internal sealed class AuthenticatedUserRequirement : ClaimFenceRequirement
{
    public override string Description => "Authenticated user";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        return user.Identity?.IsAuthenticated == true
            ? null
            : Failure("not_authenticated", "User is not authenticated");
    }
}

internal sealed class RoleRequirement : ClaimFenceRequirement
{
    private readonly string _role;

    public RoleRequirement(string role) => _role = role;

    public override string Description => $"Role '{_role}'";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        return user.IsInRole(_role)
            ? null
            : Failure("missing_role", $"Missing required role: {_role}");
    }
}

internal sealed class AnyRoleRequirement : ClaimFenceRequirement
{
    private readonly string[] _roles;

    public AnyRoleRequirement(string[] roles) => _roles = roles.ToArray();

    public override string Description => $"Any role of [{string.Join(", ", _roles)}]";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        foreach (var role in _roles)
        {
            if (user.IsInRole(role))
            {
                return null;
            }
        }

        return Failure("missing_role", $"Missing required role: one of [{string.Join(", ", _roles)}]");
    }
}

internal sealed class ClaimMatchRequirement : ClaimFenceRequirement
{
    private readonly string _claimType;
    private readonly ClaimMatch _match;

    public ClaimMatchRequirement(string claimType, ClaimMatch match)
    {
        _claimType = claimType;
        _match = match;
    }

    public override string Description => $"Claim '{_claimType}' matches {_match.Description}";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        var actual = user.FindAll(_claimType).Select(c => c.Value).ToArray();
        var outcome = _match.Evaluate(_claimType, actual, context);
        return outcome.Succeeded ? null : Failure(outcome.Code, outcome.Message);
    }
}

internal sealed class AllClaimsRequirement : ClaimFenceRequirement
{
    private readonly string _claimType;
    private readonly string[] _values;

    public AllClaimsRequirement(string claimType, string[] values)
    {
        _claimType = claimType;
        _values = values.ToArray();
    }

    public override string Description => $"Claim '{_claimType}' contains all of [{string.Join(", ", _values)}]";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        var actual = user.FindAll(_claimType).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        var missing = _values.Where(v => !actual.Contains(v)).ToArray();
        if (missing.Length == 0)
        {
            return null;
        }

        return Failure(
            "missing_claims",
            $"Missing required claims: {_claimType} must contain all of [{string.Join(", ", _values)}] but is missing [{string.Join(", ", missing)}]");
    }
}

internal sealed class ForbiddenClaimRequirement : ClaimFenceRequirement
{
    private readonly string _claimType;
    private readonly string _forbiddenValue;

    public ForbiddenClaimRequirement(string claimType, string forbiddenValue)
    {
        _claimType = claimType;
        _forbiddenValue = forbiddenValue;
    }

    public override string Description => $"Claim '{_claimType}' is not '{_forbiddenValue}'";

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        var present = user.FindAll(_claimType).Any(c => string.Equals(c.Value, _forbiddenValue, StringComparison.Ordinal));
        return present
            ? Failure("forbidden_claim", $"Forbidden claim present: {_claimType} = {_forbiddenValue}")
            : null;
    }
}

internal sealed class CustomRequirement : ClaimFenceRequirement
{
    private readonly Func<ClaimsPrincipal, ClaimMatchContext, bool> _predicate;

    public CustomRequirement(string description, Func<ClaimsPrincipal, ClaimMatchContext, bool> predicate)
    {
        Description = description;
        _predicate = predicate;
    }

    public override string Description { get; }

    public override ClaimFenceFailure? Check(ClaimsPrincipal user, ClaimMatchContext context)
    {
        return _predicate(user, context)
            ? null
            : Failure("custom_requirement_failed", Description);
    }
}
