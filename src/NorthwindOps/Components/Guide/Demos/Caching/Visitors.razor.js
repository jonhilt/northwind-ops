// Demo only: five visitors load this page at the same moment, then the page reloads to show
// how many report queries they ran between them.
const button = document.querySelector("[data-burst]");
const status = document.querySelector("[data-burst-status]");
const key = `cacheview-burst:${location.pathname}`;
const queries = () => Number(document.querySelector("[data-queries]").textContent);

const before = sessionStorage.getItem(key);
if (before !== null) {
    sessionStorage.removeItem(key);
    const ran = queries() - Number(before);
    status.textContent = `Last burst: 5 visitors, ${ran} ${ran === 1 ? "query" : "queries"} ran`;
}

button.addEventListener("click", async () => {
    button.disabled = true;
    status.textContent = "5 visitors loading…";
    const start = queries();
    await Promise.all(Array.from({ length: 5 }, () => fetch(location.href, { cache: "no-store" })));
    sessionStorage.setItem(key, String(start));
    location.reload();
});
