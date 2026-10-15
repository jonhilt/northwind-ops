// Demo only: each time blazor.web.js moves on to a new attempt, note how long it waited.
(() => {
    const out = document.querySelector("[data-gaps]");
    const attempt = document.getElementById("components-reconnect-current-attempt");
    let last = null, shown = null, runs = [];

    new MutationObserver(() => {
        const now = performance.now(), n = Number(attempt.textContent);
        if (n === shown) return; // the countdown rewrites the same number every second
        shown = n;
        if (n === 1 || last === null) { runs = []; last = now; return; }
        const gap = Math.round((now - last) / 1000);
        last = now;
        // Runs of equal gaps: "0 s ×10" reads better than ten zeros.
        if (runs.at(-1)?.gap === gap) runs.at(-1).count++;
        else runs.push({ gap, count: 1 });
        out.textContent = " · measured waits: "
            + runs.map(r => `${r.gap} s ×${r.count}`).join(", then ");
    }).observe(attempt, { childList: true, characterData: true, subtree: true });
})();
