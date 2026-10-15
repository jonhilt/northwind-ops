// Plain JS, the same under both lists. It reads the page each frame and says what's there.
const out = document.querySelector("[data-thread-readout]");
const number = article => +article.querySelector("header").textContent.match(/#(\d+)/)[1];

// Where a message starts, relative to the top of the scrolling box.
const where = (article, top) => {
    if (!article) return "not on the page";
    const offset = Math.round(article.getBoundingClientRect().top - top);
    if (Math.abs(offset) <= 1) return "at the top";
    if (offset > 0) return `starts ${offset}px below the top`;
    const tail = article.getBoundingClientRect().bottom > top + 1;
    return `header ${-offset}px above the top, out of sight` + (tail ? " (only its yellow end shows)" : "");
};

const draw = () => {
    // Looked up each time: when the circuit starts, Blazor swaps the prerendered list for a live one.
    const box = document.querySelector(".thread");
    if (box) {
        const top = box.getBoundingClientRect().top;
        const shown = [...box.querySelectorAll("article")];
        const first = shown.find(a => a.getBoundingClientRect().top >= top - 1);
        // A message cut off at the top: only its end shows, its header is out of sight.
        const cut = shown.find(a => a.getBoundingClientRect().top < top - 1 && a.getBoundingClientRect().bottom > top + 1);
        const asked = +box.dataset.asked;
        out.textContent = `Asked for #${asked}: ${where(shown.find(a => number(a) === asked), top)}` +
            (first ? ` · Top of the box: ${cut ? `the end of #${number(cut)}, then ` : ""}#${number(first)}` : "") +
            (shown.length ? ` · On the page: #${number(shown[0])}–#${number(shown.at(-1))}` +
                ` (${shown.length} of ${box.dataset.count})` : "");
    }
    requestAnimationFrame(draw);
};
requestAnimationFrame(draw);
