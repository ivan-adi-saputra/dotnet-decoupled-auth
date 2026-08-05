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
    public int FailedCount { get; private set; }

    public int RecordFailure() => ++FailedCount;

    public void Reset() => FailedCount = 0;
}
