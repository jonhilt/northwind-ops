// Demo only: waits for the cached figures to expire, has five visitors load this page at the same
// moment, then reloads to show how many report queries they ran between them.
const button = document.querySelector("[data-burst]");
const status = document.querySelector("[data-burst-status]");
const key = `cacheview-burst:${location.pathname}`;
const queries = () => Number(document.querySelector("[data-queries]").textContent);

// How old the figures were when the server sent this page (both times come from the server's clock).
const loadedAt = performance.now();
const servedAt = Date.parse(document.querySelector("[data-now]").dataset.now);
const calculatedAt = Date.parse(document.querySelector("[data-calculated]").dataset.calculated);
const ageNow = () => servedAt - calculatedAt + (performance.now() - loadedAt);
const expiresAfter = 10_000, margin = 500;

const before = sessionStorage.getItem(key);
if (before !== null) {
    sessionStorage.removeItem(key);
    const ran = queries() - Number(before);
    status.textContent = `5 visitors, ${ran} ${ran === 1 ? "query" : "queries"} ran`;
}

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

button.addEventListener("click", async () => {
    button.disabled = true;
    // Wait until the cached figures have expired, so all five visitors find the cache empty.
    while (ageNow() < expiresAfter + margin) {
        const left = Math.ceil((expiresAfter + margin - ageNow()) / 1000);
        status.textContent = `Waiting ${left} s for the cached figures to expire…`;
        await sleep(Math.min(250, expiresAfter + margin - ageNow()));
    }
    status.textContent = "5 visitors loading…";
    const start = queries();
    await Promise.all(Array.from({ length: 5 }, () => fetch(location.href, { cache: "no-store" })));
    sessionStorage.setItem(key, String(start));
    location.reload();
});
