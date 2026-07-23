const mobileEditorSelections = new WeakMap();
const trackedMobileEditors = new WeakSet();
let mobileSelectionListenerAttached = false;
let mobileClient;

function isMobileClient() {
    if (mobileClient !== undefined) {
        return mobileClient;
    }

    const userAgentData = navigator.userAgentData;
    if (userAgentData?.mobile === true) {
        mobileClient = true;
        return mobileClient;
    }

    const userAgent = navigator.userAgent || "";
    const mobileUserAgent = /Android|iPhone|iPad|iPod/i.test(userAgent);
    const iPadOsDesktopMode = navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1;

    mobileClient = mobileUserAgent || iPadOsDesktopMode;
    return mobileClient;
}

function clampSelection(selection, length) {
    const start = Math.max(0, Math.min(selection.start, length));
    const end = Math.max(start, Math.min(selection.end, length));

    return { start, end };
}

function readLiveSelection(element, fallbackLength) {
    const start = typeof element.selectionStart === "number" ? element.selectionStart : fallbackLength;
    const end = typeof element.selectionEnd === "number" ? element.selectionEnd : start;

    return clampSelection({ start, end }, fallbackLength);
}

function rememberSelection(element) {
    const valueLength = (element.value || "").length;
    mobileEditorSelections.set(element, readLiveSelection(element, valueLength));
}

function rememberRange(element, start, end) {
    const valueLength = (element.value || "").length;
    mobileEditorSelections.set(element, clampSelection({ start, end }, valueLength));
}

function getMobileInsertionSelection(element, valueLength) {
    if (document.activeElement === element) {
        const selection = readLiveSelection(element, valueLength);
        mobileEditorSelections.set(element, selection);
        return selection;
    }

    const cachedSelection = mobileEditorSelections.get(element);
    if (cachedSelection) {
        return clampSelection(cachedSelection, valueLength);
    }

    return { start: valueLength, end: valueLength };
}

function insertWithSelection(element, text, selection, shouldUpdateDomSelection) {
    const value = element.value || "";
    const { start, end } = clampSelection(selection, value.length);
    const nextValue = value.slice(0, start) + text + value.slice(end);
    const nextPosition = start + text.length;

    element.value = nextValue;

    if (shouldUpdateDomSelection) {
        try {
            element.setSelectionRange(nextPosition, nextPosition);
        } catch {
            // Some mobile browsers only allow selection changes while the editor is focused.
        }
    }

    return { nextValue, nextPosition };
}

window.logicExporter = {
    trackMobileEditor(element) {
        if (!element || !isMobileClient() || trackedMobileEditors.has(element)) {
            return;
        }

        trackedMobileEditors.add(element);

        const updateSelection = () => rememberSelection(element);
        const selectionEvents = ["focus", "input", "click", "keyup", "select", "touchend", "compositionend"];

        for (const eventName of selectionEvents) {
            element.addEventListener(eventName, updateSelection);
        }

        rememberRange(element, (element.value || "").length, (element.value || "").length);

        if (!mobileSelectionListenerAttached) {
            document.addEventListener("selectionchange", () => {
                const activeElement = document.activeElement;
                if (activeElement && trackedMobileEditors.has(activeElement)) {
                    rememberSelection(activeElement);
                }
            });

            mobileSelectionListenerAttached = true;
        }
    },

    insertAtCursor(element, text) {
        if (!element) {
            return text;
        }

        if (!isMobileClient()) {
            const value = element.value || "";
            const selection = readLiveSelection(element, value.length);
            return insertWithSelection(element, text, selection, true).nextValue;
        }

        const value = element.value || "";
        const selection = getMobileInsertionSelection(element, value.length);
        const shouldUpdateDomSelection = document.activeElement === element;
        const { nextValue, nextPosition } = insertWithSelection(element, text, selection, shouldUpdateDomSelection);

        rememberRange(element, nextPosition, nextPosition);
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
