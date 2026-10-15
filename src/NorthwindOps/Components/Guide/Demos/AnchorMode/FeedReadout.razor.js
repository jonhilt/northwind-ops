// Demo only: reads the feed from the page, the same way on both sides. Line 1: the newest message,
// the lowest you can see, and how far the list is from its bottom. Line 2: a tally of what each
// new message did, sorted by where you were when it arrived (at the bottom, or scrolled up).
const [now, tally] = document.querySelectorAll("[data-feed-readout] span");
const t = { atBottom: 0, followed: 0, away: 0, pulled: 0 };
const visible = (row, box) => {
    const r = row.getBoundingClientRect();
    return r.top < box.bottom - 1 && r.bottom > box.top + 1;
};
let last, wasAtBottom, touched = 0;
// A message that lands while you're scrolling isn't counted: the scroll would be yours.
for (const e of ["wheel", "touchmove", "keydown", "pointerdown"])
    addEventListener(e, () => touched = performance.now(), { capture: true, passive: true });
const draw = () => {
    // Looked up each time: when the circuit starts, Blazor swaps in the live feed.
    const feed = document.querySelector(".feed");
    const newest = Number(document.querySelector("[data-newest]")?.dataset.newest);
    const box = feed?.getBoundingClientRect();
    const seen = feed ? [...feed.querySelectorAll("[data-n]")].filter(r => visible(r, box)) : [];
    if (newest && seen.length) {
        const lowest = Math.max(...seen.map(r => +r.dataset.n));
        const below = Math.round(feed.scrollHeight - feed.scrollTop - feed.clientHeight);
        const atBottom = below < 2;
        const behind = newest - lowest;
        now.textContent = `Newest: #${10900 + newest} · Lowest you can see: #${10900 + lowest} · ` +
            (behind > 0 ? `${behind} newer out of sight` : "newest in view") + " · " +
            (atBottom ? "at the bottom" : `${below}px from the bottom`);
        // A message arrived: a moment later, see whether the list scrolled to it.
        if (last !== undefined && newest !== last) {
            const fromBottom = wasAtBottom, at = performance.now();
            setTimeout(() => {
                if (touched > at - 300) return;
                const f = document.querySelector(".feed");
                const there = f.scrollHeight - f.scrollTop - f.clientHeight < 2;
                if (fromBottom) { t.atBottom++; if (there) t.followed++; }
                else { t.away++; if (there) t.pulled++; }
                tally.textContent = `At the bottom: ${t.atBottom} arrived, ${t.followed} followed · ` +
                    `Scrolled up: ${t.away} arrived, ${t.pulled} pulled you down`;
            }, 600);
        }
        last = newest;
        wasAtBottom = atBottom;
    }
    requestAnimationFrame(draw);
};
requestAnimationFrame(draw);
