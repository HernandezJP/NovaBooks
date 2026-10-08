// Utilidades de interfaz de NovaBooks.
window.novaBooks = {
    // Lleva la vista al primer campo con error y le da el foco.
    scrollToFirstError: function () {
        requestAnimationFrame(function () {
            var field = document.querySelector(".mud-input-error, .validation-message");
            if (!field) { return; }
            field.scrollIntoView({ behavior: "smooth", block: "center" });
            var input = field.closest(".mud-input-control")?.querySelector("input, textarea");
            if (input) { input.focus({ preventScroll: true }); }
        });
    }
};
