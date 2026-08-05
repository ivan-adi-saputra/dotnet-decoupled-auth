using Microsoft.AspNetCore.Components;

namespace AuthApp.Client.Services;

public static class NavigationManagerExtensions
{
    /// <summary>
    /// Redirects to <paramref name="redirectTo"/> unless <paramref name="allowed"/> is true.
    /// Centralizes the "OnInitialized guard" pattern shared by Login, Welcome, and
    /// LockScreen, so each page states its access rule as one line instead of a bespoke
    /// if/NavigateTo block.
    /// </summary>
    public static void EnsureOr(this NavigationManager navigation, bool allowed, string redirectTo)
    {
        if (!allowed)
        {
            navigation.NavigateTo(redirectTo);
        }
    }
}
