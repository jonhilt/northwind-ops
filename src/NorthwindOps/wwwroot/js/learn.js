// Walkthrough pages: highlights their code, and wires animated figures (data-anim): each rests on its last
// frame, the outcome, until the reader presses Play to watch it from the start. Runs on full loads and after
// enhanced navigation.
// (guide.js wires their live demo frames.)
const reduceMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;

function play(figure, button) {
    figure.classList.remove("armed", "playing");
    void figure.offsetWidth; // restart the CSS animations
    figure.classList.add("armed", "playing");
    button.textContent = "↻ Replay";
}

function enhance() {
    const article = document.querySelector("[data-learn]");
    if (!article) return;
    window.Prism?.highlightAllUnder(article);
    if (reduceMotion) return; // the markup is already the last frame
    for (const figure of article.querySelectorAll("[data-anim]:not([data-wired])")) {
        figure.dataset.wired = "true";
        const replay = figure.querySelector("[data-replay]");
        replay.hidden = false;
        replay.textContent = "▶ Play";
        replay.addEventListener("click", () => play(figure, replay));
    }
}

enhance();
window.Blazor?.addEventListener("enhancedload", enhance);
