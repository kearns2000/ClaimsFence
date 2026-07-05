namespace ClaimsFence;

/// <summary>
/// Entry point for building claims authorisation rules.
/// </summary>
public static class ClaimFence
{
    /// <summary>
    /// Starts a new fluent <see cref="ClaimFenceBuilder"/>.
    /// </summary>
    /// <returns>A new builder.</returns>
    public static ClaimFenceBuilder Rule() => new();
}
