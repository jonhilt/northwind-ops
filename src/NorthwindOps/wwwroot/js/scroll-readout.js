// A readout for the variable-height list demos: how tall the scroll range is (and how far it has
// swung), which message is at the top, and how far Expand all moved it. Plain JS, so the same file
// runs on the .NET 11 demo and was injected into the .NET 10 repro for its recording.
(() => {
    // Looked up each time: when the circuit starts, Blazor swaps the prerendered list for a live one.
    const list = () => document.querySelector(".thread");
    if (!list()) return;
    let out = document.querySelector("[data-scroll-readout]");
    if (!out) {
        out = document.createElement("p");
        out.setAttribute("data-scroll-readout", "");
        out.style.cssText = "position:fixed;bottom:0;left:0;right:0;pointer-events:none;margin:0;padding:8px 12px;font:13px system-ui;background:#1d1b2e;color:#fff;z-index:9";
        document.body.prepend(out);
    }

    let min = Infinity, max = 0, expanded = "", seen = null, scrolling = false;
    // Count the swing only once the reader scrolls, not while the list first measures itself.
    document.addEventListener("scroll", () => { scrolling = true; }, { capture: true, once: true });
    const topMessage = () => {
        const thread = list();
        const box = thread.getBoundingClientRect();
        const first = [...thread.querySelectorAll("article")].find(a => a.getBoundingClientRect().bottom > box.top + 1);
        return first && { n: first.dataset.n, at: Math.round(first.getBoundingClientRect().top - box.top), el: first };
    };
    const px = n => n.toLocaleString("en-US") + "px";

    const draw = () => {
        seen = list();
        const height = seen.scrollHeight;
        if (scrolling) {
            min = Math.min(min, height);
            max = Math.max(max, height);
        }
        const top = topMessage();
        out.textContent = `Scroll range ${px(height)} (${scrolling ? `swung ${px(max - min)} while scrolling` : "scroll to measure"})` +
            (top ? ` · top: #${top.n}` : "") + (expanded ? ` · ${expanded}` : "");
        requestAnimationFrame(draw);
    };
    requestAnimationFrame(draw);

    // When Expand all is pressed, note where the message being read was, and where it ends up.
    document.addEventListener("click", e => {
        if (!e.target.closest("button")?.textContent.includes("Expand all")) return;
        const before = topMessage();
        if (!before) return;
        const startY = before.el.getBoundingClientRect().top;
        setTimeout(() => {
            const el = list().querySelector(`article[data-n="${before.n}"]`);
            const moved = el ? Math.round(el.getBoundingClientRect().top - startY) : null;
            expanded = moved === null ? `Expand all: #${before.n} scrolled out of view`
                : `Expand all moved #${before.n} by ${px(Math.abs(moved))}`;
        }, 1000);
    }, true);
})();
