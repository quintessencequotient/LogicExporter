window.logicExporter = {
    insertAtCursor(element, text) {
        if (!element) {
            return text;
        }

        const value = element.value || "";
        const start = element.selectionStart ?? value.length;
        const end = element.selectionEnd ?? value.length;
        const nextValue = value.slice(0, start) + text + value.slice(end);
        const nextPosition = start + text.length;

        element.value = nextValue;

        try {
            element.setSelectionRange(nextPosition, nextPosition);
        } catch {
            // Some mobile browsers only allow selection changes while the editor is focused.
        }

        return nextValue;
    },

    async copyText(text) {
        if (!text) {
            return false;
        }

        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }

        const fallback = document.createElement("textarea");
        fallback.value = text;
        fallback.setAttribute("readonly", "");
        fallback.style.position = "fixed";
        fallback.style.opacity = "0";
        document.body.appendChild(fallback);
        fallback.select();

        try {
            return document.execCommand("copy");
        } finally {
            document.body.removeChild(fallback);
        }
    }
};
