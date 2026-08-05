using Microsoft.AspNetCore.Components;

namespace AuthApp.Client.Services;

public static class NavigationManagerExtensions
{
    /// <summary>
    /// Redirects to <paramref name="redirectTo"/> unless <paramref name="allowed"/> is true.
    /// Centralizes the "OnInitialized guard" pattern shared by Login, Register, Welcome,
    /// and LockScreen, so each page states its access rule as one line instead of a bespoke
    /// if/NavigateTo block. Returns whether the caller is allowed to proceed, so a page with
    /// more than one guard in sequence can short-circuit after the first redirect instead of
    /// falling through to checks that no longer apply.
    /// </summary>
    public static bool EnsureOr(this NavigationManager navigation, bool allowed, string redirectTo)
    {
        if (!allowed)
        {
            navigation.NavigateTo(redirectTo);
            return false;
        }

        return true;
    }
}
