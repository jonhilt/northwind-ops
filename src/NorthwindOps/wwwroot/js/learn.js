// Walkthrough pages: highlights their code, on full loads and after enhanced navigation.
// (guide.js wires their live demo frames.)
function enhance() {
    const article = document.querySelector("[data-learn]");
    if (article) window.Prism?.highlightAllUnder(article);
}

enhance();
window.Blazor?.addEventListener("enhancedload", enhance);
