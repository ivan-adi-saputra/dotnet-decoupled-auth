// Thin wrapper around SweetAlert2 so C# only calls two simple functions via JS interop.
// Uses "toast" mode: a small, non-blocking, auto-dismissing notification that SweetAlert2
// appends directly to document.body — outside Blazor's managed DOM tree — so it keeps
// showing and dismissing itself on its own timer even if Blazor navigates away
// immediately afterward.
//
// Messages are passed via SweetAlert2's `text` option, not `title`: `title` renders its
// content as HTML, while `text` always renders as plain text (.textContent), regardless of
// what the message contains. This matters because these messages can embed server-echoed
// user input (e.g. "Username 'x' is already taken.") — proven live that a username of
// "<svg/onload=alert(1)>" got parsed as real HTML when passed via `title`. The backend now
// also rejects that input outright (username charset restriction), but this is kept as a
// second, independent layer: any future message that echoes user input still can't inject
// markup through this code path.
window.showSuccessToast = function (message) {
    Swal.fire({
        icon: "success",
        text: message,
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 2500,
        timerProgressBar: true
    });
};

window.showErrorToast = function (message) {
    Swal.fire({
        icon: "error",
        text: message,
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
};
