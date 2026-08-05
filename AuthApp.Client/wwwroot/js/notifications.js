// Thin wrapper around SweetAlert2 so C# only calls two simple functions via JS interop.
// Uses "toast" mode: a small, non-blocking, auto-dismissing notification that SweetAlert2
// appends directly to document.body — outside Blazor's managed DOM tree — so it keeps
// showing and dismissing itself on its own timer even if Blazor navigates away
// immediately afterward.
window.showSuccessToast = function (message) {
    Swal.fire({
        icon: "success",
        title: message,
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
        title: message,
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
};
