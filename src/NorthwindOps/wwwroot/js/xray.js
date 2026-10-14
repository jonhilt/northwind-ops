// X-ray mode: outlines every .NET 11 feature on the page and opens a drawer with its code.
// Plain JavaScript on purpose: it has to work on static SSR pages that never start a circuit.

const root = document.documentElement;
const storageKey = "northwind-ops:xray";

function setXRay(on) {
    root.classList.toggle("xray-on", on);
    localStorage.setItem(storageKey, on ? "on" : "off");
    document.querySelectorAll("[data-xray-toggle]").forEach(b => b.setAttribute("aria-pressed", String(on)));
}

function drawer() {
    return document.getElementById("xray-drawer");
}

async function openPanel(id) {
    const el = drawer();
    if (!el) return;
    const response = await fetch(new URL(`xray/${encodeURIComponent(id)}/panel`, document.baseURI));
    if (!response.ok) return;
    el.innerHTML = await response.text();
    el.classList.add("is-open");
    el.setAttribute("aria-hidden", "false");
    el.scrollTop = 0;
    highlight(el);
    el.querySelector("[data-xray-close]")?.focus();
}

// Syntax-highlights the code, then scrolls each listing to the lines that show the feature.
function highlight(container) {
    if (!container || !window.Prism) return;
    window.Prism.highlightAllUnder(container);
    container.querySelectorAll("pre").forEach(pre => {
        const first = pre.querySelector(".line-highlight");
        if (first) pre.scrollTop = Math.max(0, first.offsetTop - 60);
    });
}

function closePanel() {
    const el = drawer();
    if (!el) return;
    el.classList.remove("is-open");
    el.setAttribute("aria-hidden", "true");
}

function selectTab(button) {
    const panel = button.closest(".xr-panel");
    panel.querySelectorAll("[data-xr-tab]").forEach(b => b.setAttribute("aria-selected", String(b === button)));
    panel.querySelectorAll("[data-xr-pane]").forEach(p => {
        p.hidden = p.dataset.xrPane !== button.dataset.xrTab;
    });
    // Line highlights are positioned when shown, so redraw them for the newly visible pane.
    highlight(panel.querySelector(`[data-xr-pane="${button.dataset.xrTab}"]`));
}

// Badge clicks are caught on the way down (capture phase on window), before Blazor's enhanced
// navigation sees the link and navigates to the full /xray/{id} page. That page is still there
// for new tabs and visitors without JavaScript.
window.addEventListener("click", event => {
    const open = event.target instanceof Element ? event.target.closest("[data-xray-open]") : null;
    if (open && !event.ctrlKey && !event.metaKey && !event.shiftKey && event.button === 0) {
        event.preventDefault();
        event.stopPropagation();
        openPanel(open.dataset.xrayOpen);
    }
}, { capture: true });

document.addEventListener("click", event => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target) return;

    if (target.closest("[data-xray-toggle]")) {
        setXRay(!root.classList.contains("xray-on"));
        return;
    }
    if (target.closest("[data-xray-close]")) {
        closePanel();
        return;
    }
    const tab = target.closest("[data-xr-tab]");
    if (tab) {
        selectTab(tab);
    }
});

document.addEventListener("keydown", event => {
    if (event.key === "Escape") {
        closePanel();
        return;
    }
    const typing = event.target instanceof Element && event.target.closest("input, textarea, select, [contenteditable]");
    if (!typing && !event.ctrlKey && !event.metaKey && !event.altKey && event.key.toLowerCase() === "x") {
        setXRay(!root.classList.contains("xray-on"));
    }
});

// Code on a standalone /xray/{id} page is highlighted in place.
function highlightPage() {
    document.querySelectorAll(".xr-page").forEach(highlight);
}

// Restore the last choice, and again after enhanced navigation swaps the page content.
setXRay(localStorage.getItem(storageKey) === "on");
highlightPage();
window.Blazor?.addEventListener("enhancedload", () => {
    setXRay(localStorage.getItem(storageKey) === "on");
    closePanel();
    highlightPage();
});
