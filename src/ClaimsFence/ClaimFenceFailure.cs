namespace ClaimsFence;

/// <summary>
/// Describes a single reason a <see cref="ClaimFenceRule"/> did not pass evaluation.
/// A failure carries a stable <see cref="Code"/> for programmatic checks and a
/// human readable <see cref="Message"/> for logs, tests, and diagnostics.
/// </summary>
public sealed class ClaimFenceFailure
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ClaimFenceFailure"/> class.
    /// </summary>
    /// <param name="code">A stable, machine readable identifier for the failure kind.</param>
    /// <param name="message">A human readable explanation of the failure.</param>
    /// <param name="requirement">A description of the requirement that produced the failure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="code"/>, <paramref name="message"/>, or
    /// <paramref name="requirement"/> is <see langword="null"/>.
    /// </exception>
    public ClaimFenceFailure(string code, string message, string requirement)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
    }

    /// <summary>
    /// Gets a stable, machine readable identifier for the failure kind
    /// (for example <c>missing_claim</c> or <c>not_authenticated</c>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets a human readable explanation of the failure, suitable for logs and tests.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a description of the requirement that produced the failure.
    /// </summary>
    public string Requirement { get; }

    /// <summary>
    /// Returns the failure <see cref="Message"/>.
    /// </summary>
    /// <returns>The human readable failure message.</returns>
    public override string ToString() => Message;
}
