namespace ClaimsFence;

/// <summary>
/// Options that control how a claim fence behaves, in particular how much detail is
/// exposed when a rule fails during ASP.NET Core authorisation.
/// </summary>
public sealed class ClaimFenceOptions
{
    /// <summary>
    /// When <see langword="true"/>, the detailed failure messages produced by a rule are
    /// attached to the ASP.NET Core authorisation failure as failure-reason entries
    /// (for example <c>AuthorizationFailureReason</c> in ASP.NET Core).
    /// <para>
    /// This is intended for development diagnostics, logging, and tests. It is
    /// <see langword="false"/> by default so that authorisation failure details are not
    /// leaked to callers unless you opt in.
    /// </para>
    /// </summary>
    public bool IncludeFailureMessagesInAuthorizationFailure { get; set; }
}
