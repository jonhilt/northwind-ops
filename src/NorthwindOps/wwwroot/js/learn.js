// Walkthrough pages: highlights their code, and plays animated figures (data-anim) when they first
// scroll into view, or from their replay button. Runs on full loads and after enhanced navigation.
// (guide.js wires their live demo frames.)
const reduceMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;

function play(figure) {
    figure.classList.remove("playing");
    void figure.offsetWidth; // restart the CSS animations
    figure.classList.add("playing");
}

const seen = new IntersectionObserver(entries => {
    for (const entry of entries.filter(e => e.isIntersecting)) {
        seen.unobserve(entry.target);
        play(entry.target);
    }
}, { threshold: 0.5 });

function enhance() {
    const article = document.querySelector("[data-learn]");
    if (!article) return;
    window.Prism?.highlightAllUnder(article);
    if (reduceMotion) return; // the markup is already the last frame
    for (const figure of article.querySelectorAll("[data-anim]:not([data-wired])")) {
        figure.dataset.wired = "true";
        const replay = figure.querySelector("[data-replay]");
        replay.hidden = false;
        replay.addEventListener("click", () => play(figure));
        seen.observe(figure);
    }
}

enhance();
window.Blazor?.addEventListener("enhancedload", enhance);
