namespace AuthApp.Client.Services;

/// <summary>
/// Tracks consecutive failed login attempts for the whole app session (not just one
/// component instance) — registered as a scoped/app-lifetime service so navigating to
/// Register and back to Login no longer silently resets the count. It only resets on a
/// successful login or when the WASM app itself restarts (a real browser reload),
/// matching "Start -> LoginFailed = 0" on the flowchart more faithfully than a
/// component-local field would.
/// </summary>
public class LoginAttemptTracker
{
    /// <summary>
    /// The flowchart's "LoginFailed > 3" threshold — the single source of truth for the
    /// lockout boundary, so Login and LockScreen can't drift out of sync by comparing
    /// against a magic number copied into each page separately.
    /// </summary>
    public const int MaxFailedAttempts = 3;

    public int FailedCount { get; private set; }

    public bool IsLockedOut => FailedCount > MaxFailedAttempts;

    public int RecordFailure() => ++FailedCount;

    public void Reset() => FailedCount = 0;
}
