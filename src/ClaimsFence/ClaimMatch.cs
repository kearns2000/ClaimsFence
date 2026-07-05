namespace ClaimsFence;

/// <summary>
/// Describes how the value of a claim should be matched. A match can compare against a
/// static value, a set of allowed values, a predicate, or a value resolved at evaluation
/// time from a route value, header, or query string.
/// </summary>
public sealed class ClaimMatch
{
    private enum MatchKind
    {
        Value,
        Any,
        Predicate,
        Contextual,
    }

    private readonly MatchKind _kind;
    private readonly string? _expectedValue;
    private readonly IReadOnlyList<string>? _anyValues;
    private readonly Func<string, bool>? _predicate;
    private readonly ClaimValueSource _source;
    private readonly string? _contextKey;

    private ClaimMatch(
        MatchKind kind,
        string description,
        string? expectedValue = null,
        IReadOnlyList<string>? anyValues = null,
        Func<string, bool>? predicate = null,
        ClaimValueSource source = ClaimValueSource.Route,
        string? contextKey = null)
    {
        _kind = kind;
        Description = description;
        _expectedValue = expectedValue;
        _anyValues = anyValues;
        _predicate = predicate;
        _source = source;
        _contextKey = contextKey;
    }

    /// <summary>
    /// Gets a human readable description of what this match expects.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Matches a claim whose value equals the supplied value (ordinal, case-sensitive comparison).
    /// </summary>
    /// <param name="expectedValue">The expected claim value.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedValue"/> is <see langword="null"/>.</exception>
    public static ClaimMatch Value(string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);
        return new ClaimMatch(MatchKind.Value, $"value '{expectedValue}'", expectedValue: expectedValue);
    }

    /// <summary>
    /// Matches a claim whose value equals any of the supplied values.
    /// </summary>
    /// <param name="values">The allowed values.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is null, empty, or contains a null or empty value.</exception>
    public static ClaimMatch Any(params string[] values)
    {
        var validated = ClaimFenceGuard.RequireTextArray(values, nameof(values));
        return new ClaimMatch(MatchKind.Any, $"any of [{string.Join(", ", validated)}]", anyValues: validated);
    }

    /// <summary>
    /// Matches a claim whose value satisfies the supplied predicate.
    /// </summary>
    /// <param name="description">A human readable description of the predicate, used in diagnostics.</param>
    /// <param name="predicate">The predicate applied to the claim value.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="description"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ClaimMatch Predicate(string description, Func<string, bool> predicate)
    {
        ClaimFenceGuard.RequireText(description, nameof(description));
        ArgumentNullException.ThrowIfNull(predicate);
        return new ClaimMatch(MatchKind.Predicate, description, predicate: predicate);
    }

    /// <summary>
    /// Matches a claim whose value equals a route value resolved at evaluation time.
    /// </summary>
    /// <param name="key">The route value key.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public static ClaimMatch RouteValue(string key) => Contextual(ClaimValueSource.Route, key, "route value");

    /// <summary>
    /// Matches a claim whose value equals an HTTP header resolved at evaluation time.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public static ClaimMatch Header(string name) => Contextual(ClaimValueSource.Header, name, "header");

    /// <summary>
    /// Matches a claim whose value equals a query string value resolved at evaluation time.
    /// </summary>
    /// <param name="key">The query string key.</param>
    /// <returns>A new <see cref="ClaimMatch"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public static ClaimMatch QueryString(string key) => Contextual(ClaimValueSource.QueryString, key, "query string value");

    private static ClaimMatch Contextual(ClaimValueSource source, string key, string label)
    {
        ClaimFenceGuard.RequireText(key, nameof(key));
        return new ClaimMatch(MatchKind.Contextual, $"{label} '{key}'", source: source, contextKey: key);
    }

    internal ClaimMatchOutcome Evaluate(string claimType, IReadOnlyCollection<string> actualValues, ClaimMatchContext context)
    {
        switch (_kind)
        {
            case MatchKind.Value:
                return EvaluateEquals(claimType, _expectedValue!, actualValues);

            case MatchKind.Any:
                return EvaluateAny(claimType, actualValues);

            case MatchKind.Predicate:
                return EvaluatePredicate(claimType, actualValues);

            case MatchKind.Contextual:
                if (!context.TryResolve(_source, _contextKey!, out var resolved) || resolved is null)
                {
                    var sourceName = _source switch
                    {
                        ClaimValueSource.Route => "route value",
                        ClaimValueSource.Header => "header",
                        ClaimValueSource.QueryString => "query string value",
                        _ => "value",
                    };

                    return ClaimMatchOutcome.Fail(
                        "unresolved_match_value",
                        $"Cannot resolve {sourceName} '{_contextKey}' for claim '{claimType}'. Supply it through a ClaimMatchContext or evaluate inside ASP.NET Core.");
                }

                return EvaluateEquals(claimType, resolved, actualValues);

            default:
                throw new InvalidOperationException($"Unknown match kind '{_kind}'.");
        }
    }

    private static ClaimMatchOutcome EvaluateEquals(string claimType, string expected, IReadOnlyCollection<string> actualValues)
    {
        foreach (var value in actualValues)
        {
            if (string.Equals(value, expected, StringComparison.Ordinal))
            {
                return ClaimMatchOutcome.Success;
            }
        }

        if (actualValues.Count == 0)
        {
            return ClaimMatchOutcome.Fail("missing_claim", $"Missing required claim: {claimType} = {expected}");
        }

        return ClaimMatchOutcome.Fail(
            "claim_mismatch",
            $"Claim mismatch: {claimType} expected {expected} but found {string.Join(", ", actualValues)}");
    }

    private ClaimMatchOutcome EvaluateAny(string claimType, IReadOnlyCollection<string> actualValues)
    {
        foreach (var value in actualValues)
        {
            foreach (var allowed in _anyValues!)
            {
                if (string.Equals(value, allowed, StringComparison.Ordinal))
                {
                    return ClaimMatchOutcome.Success;
                }
            }
        }

        var allowedList = string.Join(", ", _anyValues!);
        if (actualValues.Count == 0)
        {
            return ClaimMatchOutcome.Fail("missing_claim", $"Missing required claim: {claimType} = one of [{allowedList}]");
        }

        return ClaimMatchOutcome.Fail(
            "claim_mismatch",
            $"Claim mismatch: {claimType} expected one of [{allowedList}] but found {string.Join(", ", actualValues)}");
    }

    private ClaimMatchOutcome EvaluatePredicate(string claimType, IReadOnlyCollection<string> actualValues)
    {
        foreach (var value in actualValues)
        {
            if (_predicate!(value))
            {
                return ClaimMatchOutcome.Success;
            }
        }

        if (actualValues.Count == 0)
        {
            return ClaimMatchOutcome.Fail("missing_claim", $"Missing required claim: {claimType} ({Description})");
        }

        return ClaimMatchOutcome.Fail(
            "claim_predicate_failed",
            $"Claim predicate failed: {claimType} ({Description}) but found {string.Join(", ", actualValues)}");
    }
}

/// <summary>
/// The internal outcome of evaluating a single <see cref="ClaimMatch"/>.
/// </summary>
internal readonly struct ClaimMatchOutcome
{
    private ClaimMatchOutcome(bool succeeded, string code, string message)
    {
        Succeeded = succeeded;
        Code = code;
        Message = message;
    }

    public bool Succeeded { get; }

    public string Code { get; }

    public string Message { get; }

    public static ClaimMatchOutcome Success { get; } = new(true, string.Empty, string.Empty);

    public static ClaimMatchOutcome Fail(string code, string message) => new(false, code, message);
}
