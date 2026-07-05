namespace ClaimsFence;

/// <summary>
/// The immutable outcome of evaluating a <see cref="ClaimFenceRule"/> against a user.
/// </summary>
public sealed class ClaimFenceResult
{
    private static readonly IReadOnlyList<ClaimFenceFailure> NoFailures = Array.Empty<ClaimFenceFailure>();

    private ClaimFenceResult(bool succeeded, IReadOnlyList<ClaimFenceFailure> failures)
    {
        Succeeded = succeeded;
        Failures = failures;
    }

    /// <summary>
    /// Gets a value indicating whether every requirement in the rule passed.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the list of failures. Empty when <see cref="Succeeded"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<ClaimFenceFailure> Failures { get; }

    /// <summary>
    /// Gets a successful result with no failures.
    /// </summary>
    public static ClaimFenceResult Success { get; } = new(true, NoFailures);

    /// <summary>
    /// Creates a failed result from the supplied failures.
    /// </summary>
    /// <param name="failures">The failures that caused evaluation to fail.</param>
    /// <returns>A failed <see cref="ClaimFenceResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="failures"/> is empty.</exception>
    public static ClaimFenceResult Failed(IReadOnlyList<ClaimFenceFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count == 0)
        {
            throw new ArgumentException("A failed result must contain at least one failure.", nameof(failures));
        }

        return new ClaimFenceResult(false, failures.ToArray());
    }
}
