// Demo only: what this page told blazor.web.js about reconnecting, and whether Blazor started.
(() => {
    const box = document.querySelector("[data-sent]");
    const errors = [];
    addEventListener("error", e => errors.push(e.message));

    // .NET 11: <ConfigureBrowser> writes <!--Blazor-Configuration:base64 JSON--> into the page.
    function fromComment() {
        const walk = document.createTreeWalker(document, NodeFilter.SHOW_COMMENT);
        while (walk.nextNode()) {
            const text = walk.currentNode.data;
            if (text.startsWith("Blazor-Configuration:")) {
                const options = JSON.parse(atob(text.slice("Blazor-Configuration:".length)));
                return `<!--Blazor-Configuration:…--> decoded, its "server" part.\n`
                    + `The C# names, camelCased; the TimeSpan as milliseconds:\n`
                    + JSON.stringify(options.server, null, 2);
            }
        }
        return null;
    }

    function show() {
        // .NET 10: the hand-written Blazor.start() script, as the server rendered it.
        const start = document.querySelector("script[data-start]");
        box.querySelector("[data-sent-code]").textContent = start
            ? start.textContent.replace(/^\s*\n|\s+$/g, "").replace(/^ {12}/gm, "")
            : fromComment() ?? "Nothing: Blazor's defaults.";
    }

    function started() {
        const yes = !!window.Blazor?._internal?.forceCloseConnection;
        box.querySelector("[data-sent-started]").textContent = yes
            ? "Blazor started."
            : `Blazor didn't start.${errors.length ? ` The browser says: ${errors[0]}` : ""}`;
        box.dataset.started = yes;
    }

    document.addEventListener("DOMContentLoaded", show);
    addEventListener("load", () => setTimeout(started, 1500));
})();
