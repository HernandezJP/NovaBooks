// Utilidades de interfaz de TikalLibros.
(function () {
    // Caracteres permitidos por tipo de campo. Se aplican con la clase
    // "tl-filter-<tipo>" en el componente (MudTextField Class="...").
    // La API vuelve a validar todo; esto solo evita escribir lo que no aplica.
    var allowed = {
        digits: /[0-9]/,
        phone: /[0-9+\-() ]/,
        nit: /[0-9kK\-]/,
        dpi: /[0-9 \-]/,
        name: /[\p{L}\p{M}'’.\- ]/u,
        business: /[\p{L}\p{M}\p{N} .,&'’\-()/]/u,
        username: /[A-Za-z0-9._\-]/,
        series: /[A-Za-z0-9\-]/,
        location: /[\p{L}\p{M}0-9 .\-/#]/u,
        ident: /[A-Za-z0-9\- ]/,
        isbn: /[0-9Xx\- ]/,
        email: /[A-Za-z0-9._%+\-@]/,
        url: /[A-Za-z0-9:/?#\[\]@!$&'()*+,;=._~%\-]/,
        date: /[0-9/]/,
        plain: /[^<>]/
    };

    function filterFor(target) {
        var host = target && target.closest ? target.closest("[class*='tl-filter-']") : null;
        if (!host) { return null; }
        var match = /(?:^|\s)tl-filter-([a-z]+)/.exec(host.className);
        return match && allowed[match[1]] ? allowed[match[1]] : null;
    }

    function clean(text, pattern) {
        var result = "";
        for (var character of text) {
            if (pattern.test(character)) { result += character; }
        }
        return result;
    }

    document.addEventListener("beforeinput", function (event) {
        var pattern = filterFor(event.target);
        if (!pattern || !event.inputType || !event.inputType.startsWith("insert")) { return; }

        var text = event.data;
        if (text == null && event.dataTransfer) { text = event.dataTransfer.getData("text/plain"); }
        if (text == null) { return; }

        var cleaned = clean(text, pattern);
        if (cleaned === text) { return; }

        event.preventDefault();
        if (cleaned.length > 0) {
            // Inserta solo lo permitido y dispara los eventos normales.
            document.execCommand("insertText", false, cleaned);
        }
        showHint(event.target);
    }, true);

    // Fechas: solo dígitos, con las barras de dd/mm/aaaa colocadas al escribir.
    document.addEventListener("input", function (event) {
        var input = event.target;
        if (!(input instanceof HTMLInputElement) || !input.closest(".tl-filter-date")) { return; }

        var digits = input.value.replace(/\D/g, "").slice(0, 8);
        var formatted = digits.length > 4
            ? digits.slice(0, 2) + "/" + digits.slice(2, 4) + "/" + digits.slice(4)
            : digits.length > 2 ? digits.slice(0, 2) + "/" + digits.slice(2) : digits;

        if (formatted !== input.value) {
            input.value = formatted;
            input.setSelectionRange(formatted.length, formatted.length);
        }
    }, true);

    // Breve aviso visual cuando se descarta un carácter.
    function showHint(input) {
        var host = input.closest(".mud-input-control") || input;
        host.classList.remove("tl-filter-rejected");
        void host.offsetWidth;
        host.classList.add("tl-filter-rejected");
        setTimeout(function () { host.classList.remove("tl-filter-rejected"); }, 600);
    }

    window.tikalLibros = {
        // Descarga un texto (CSV de los reportes) como archivo. El BOM hace
        // que Excel abra bien las tildes.
        downloadText: function (fileName, content, mimeType) {
            var blob = new Blob([String.fromCharCode(0xFEFF) + content], { type: mimeType || "text/csv;charset=utf-8" });
            var url = URL.createObjectURL(blob);
            var link = document.createElement("a");
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
        },

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
})();
