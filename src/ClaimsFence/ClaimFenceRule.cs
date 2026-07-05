using System.Security.Claims;

namespace ClaimsFence;

/// <summary>
/// An immutable, reusable claims authorisation rule composed of one or more requirements.
/// Build one with <see cref="ClaimFence.Rule"/> and evaluate it with
/// <see cref="Evaluate(ClaimsPrincipal)"/>.
/// </summary>
public sealed class ClaimFenceRule
{
    private readonly ClaimFenceRequirement[] _requirements;

    internal ClaimFenceRule(ClaimFenceRequirement[] requirements)
    {
        _requirements = requirements.ToArray();
    }

    /// <summary>
    /// Gets the requirements that make up this rule, in the order they were added.
    /// </summary>
    public IReadOnlyList<ClaimFenceRequirement> Requirements => Array.AsReadOnly(_requirements);

    /// <summary>
    /// Evaluates the rule against a user with no external context.
    /// </summary>
    /// <param name="user">The user to evaluate.</param>
    /// <returns>The evaluation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public ClaimFenceResult Evaluate(ClaimsPrincipal user) => Evaluate(user, new ClaimMatchContext());

    /// <summary>
    /// Evaluates the rule against a user using the supplied external context.
    /// </summary>
    /// <param name="user">The user to evaluate.</param>
    /// <param name="context">External values available to contextual matches.</param>
    /// <returns>The evaluation result. Never throws for a normal authorisation failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="user"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public ClaimFenceResult Evaluate(ClaimsPrincipal user, ClaimMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(context);

        List<ClaimFenceFailure>? failures = null;
        foreach (var requirement in _requirements)
        {
            var failure = requirement.Check(user, context);
            if (failure is not null)
            {
                failures ??= new List<ClaimFenceFailure>();
                failures.Add(failure);
            }
        }

        return failures is null ? ClaimFenceResult.Success : ClaimFenceResult.Failed(failures);
    }
}
