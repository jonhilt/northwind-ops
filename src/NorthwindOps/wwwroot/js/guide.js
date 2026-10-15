// The walkthroughs' demo frames: the address bar follows the demo, the frame grows to fit it, and the
// refresh and Start over buttons work. The demos work without any of it.

// Shows the frame's current address, which changes on enhanced navigation as well as on full loads.
function wireDemo(box) {
    const frame = box.querySelector("[data-demo-frame]");
    const address = box.querySelector("[data-demo-address]");
    if (!frame || frame.dataset.wired) return;
    frame.dataset.wired = "true";

    const show = () => {
        try {
            const location = frame.contentWindow.location;
            if (location.href !== "about:blank") address.textContent = "\u200E" + location.pathname + location.search; // keeps the "/" first in its right-to-left box
        } catch { /* not same-origin: leave the address as it was */ }
    };
    // Grow the frame to fit the demo, including errors and messages that appear without a navigation.
    let resizer;
    const fit = () => {
        const content = frame.contentDocument?.querySelector("[data-demo-body]");
        if (!content) return;
        const height = Math.ceil(content.getBoundingClientRect().height) + 40;
        frame.style.height = `${Math.min(Math.max(height, 360), 720)}px`;
    };
    const follow = () => {
        show();
        fit();
        resizer?.disconnect();
        const content = frame.contentDocument?.querySelector("[data-demo-body]");
        if (content) {
            resizer = new ResizeObserver(fit);
            resizer.observe(content);
        }
    };
    const watch = () => {
        follow();
        frame.contentWindow.Blazor?.addEventListener?.("enhancedload", follow);
        frame.contentWindow.addEventListener("popstate", show);
    };
    frame.addEventListener("load", watch);
    if (frame.contentDocument?.readyState === "complete") watch();

    box.querySelector("[data-demo-refresh]")?.addEventListener("click", () => frame.contentWindow.location.reload());
    box.querySelector("[data-demo-reset]")?.addEventListener("click", () => { frame.src = frame.getAttribute("src"); });
}

function enhance() {
    document.querySelectorAll("[data-learn] [data-demo]").forEach(wireDemo);
}

enhance();
window.Blazor?.addEventListener("enhancedload", enhance);
