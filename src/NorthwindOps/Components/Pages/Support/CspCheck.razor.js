// The Support pages' Content-Security-Policy, from inside the tab.

const supportPath = new URL("support", document.baseURI).pathname;
const isSupport = url => {
    const path = new URL(url, document.baseURI).pathname;
    return path === supportPath || path.startsWith(supportPath + "/");
};

// A policy can't be taken off a document once it's applied. So when you leave Support, make it a full page
// load: the next screen gets a fresh document with its own policy, instead of inheriting this one.
export function keepPolicyToSupport() {
    if (window.northwindSupportPolicyGuard) return;
    window.northwindSupportPolicyGuard = true;

    // Capture phase on window runs before Blazor's own link handling, which skips links marked data-enhance-nav="false".
    window.addEventListener("click", event => {
        const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
        if (link && !link.hasAttribute("data-xray-open") && !isSupport(link.href)) {
            link.setAttribute("data-enhance-nav", "false");
        }
    }, { capture: true });

    // Back or Forward out of Support: reload the page you've arrived at.
    window.addEventListener("popstate", () => {
        if (!isSupport(location.href)) location.reload();
    });
}

export function check() {
    // An inline style attribute, as a component that rendered style="..." would produce. Under style-src 'self'
    // the browser refuses it (and logs one violation), so the element keeps its normal full width.
    const probe = document.createElement("div");
    probe.setAttribute("style", "width: 123px; height: 0; overflow: hidden");
    document.body.append(probe);
    const inlineStyleBlocked = Math.round(probe.getBoundingClientRect().width) !== 123;
    probe.remove();

    // Virtualize puts each spacer's height in data-blazor-virtualize-reserved-height, and its own script applies
    // it through the CSS Object Model (element.style.height), which a CSP doesn't restrict.
    const spacers = [...document.querySelectorAll("[data-blazor-virtualize-reserved-height]")];
    const reserved = spacers.map(s => Number(s.dataset.blazorVirtualizeReservedHeight));
    return {
        inlineStyleBlocked,
        spacers: spacers.length,
        spacersSized: spacers.filter((s, i) => Math.abs(s.getBoundingClientRect().height - reserved[i]) < 1).length,
        reservedPixels: reserved.reduce((sum, px) => sum + px, 0),
    };
}
