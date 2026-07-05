namespace ClaimsFence;

/// <summary>
/// Identifies where a contextual <see cref="ClaimMatch"/> expects to read its value from.
/// </summary>
public enum ClaimValueSource
{
    /// <summary>A route value (for example a value from <c>/tenants/{tenantId}</c>).</summary>
    Route,

    /// <summary>An HTTP request header.</summary>
    Header,

    /// <summary>An HTTP query string value.</summary>
    QueryString,
}

/// <summary>
/// Supplies external values (route values, headers, query string values, or arbitrary
/// named values) to a <see cref="ClaimFenceRule"/> during evaluation.
/// <para>
/// Inside ASP.NET Core the integration populates this context from the current HTTP request.
/// Outside ASP.NET Core, for example in
/// background workers, queue consumers, or tests, you supply the values yourself with the
/// <c>With*</c> methods.
/// </para>
/// </summary>
public sealed class ClaimMatchContext
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _routeValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _headers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _queryValues = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a generic named value. Generic values are used as a fallback for route, header,
    /// and query string matches when a source specific value is not present.
    /// </summary>
    /// <param name="key">The value key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The same <see cref="ClaimMatchContext"/> for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public ClaimMatchContext WithValue(string key, string? value)
    {
        _values[Require(key)] = value;
        return this;
    }

    /// <summary>Adds a route value.</summary>
    /// <param name="key">The route value key.</param>
    /// <param name="value">The route value.</param>
    /// <returns>The same <see cref="ClaimMatchContext"/> for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public ClaimMatchContext WithRouteValue(string key, string? value)
    {
        _routeValues[Require(key)] = value;
        return this;
    }

    /// <summary>Adds an HTTP header value.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The same <see cref="ClaimMatchContext"/> for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public ClaimMatchContext WithHeaderValue(string key, string? value)
    {
        _headers[Require(key)] = value;
        return this;
    }

    /// <summary>Adds a query string value.</summary>
    /// <param name="key">The query string key.</param>
    /// <param name="value">The query string value.</param>
    /// <returns>The same <see cref="ClaimMatchContext"/> for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public ClaimMatchContext WithQueryValue(string key, string? value)
    {
        _queryValues[Require(key)] = value;
        return this;
    }

    /// <summary>
    /// Attempts to resolve a value for the given source and key. Source specific values take
    /// priority, falling back to generic values added with <see cref="WithValue"/>.
    /// </summary>
    /// <param name="source">The source to resolve from.</param>
    /// <param name="key">The value key.</param>
    /// <param name="value">The resolved value, if found.</param>
    /// <returns><see langword="true"/> if a value was found; otherwise <see langword="false"/>.</returns>
    public bool TryResolve(ClaimValueSource source, string key, out string? value)
    {
        var sourceValues = source switch
        {
            ClaimValueSource.Route => _routeValues,
            ClaimValueSource.Header => _headers,
            ClaimValueSource.QueryString => _queryValues,
            _ => _values,
        };

        if (sourceValues.TryGetValue(key, out value) && value is not null)
        {
            return true;
        }

        return _values.TryGetValue(key, out value) && value is not null;
    }

    private static string Require(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        }

        return key;
    }
}
