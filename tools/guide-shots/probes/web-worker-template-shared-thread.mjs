// Checks the guide's claim that both frames share the tab's UI thread: while the left side totals
// on its UI thread, the right frame draws no animation frames either.
// Usage: node probes/web-worker-template-shared-thread.mjs <baseUrl>
import { chromium } from "playwright";
const [base = "http://localhost:5452"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/web-worker-template/try`, { waitUntil: "networkidle" });
const left = page.frameLocator("[data-side=before] iframe");
const right = page.frames().find(f => f.url().endsWith("/demo/web-worker/order-lines"));
for (const f of [left, page.frameLocator("[data-side=after] iframe")])
  await f.locator("[role=status]", { hasText: "Ready" }).waitFor({ timeout: 120000 });
await right.evaluate(() => {
  window.__frames = [];
  const loop = t => { window.__frames.push(t); window.__raf = requestAnimationFrame(loop); };
  window.__raf = requestAnimationFrame(loop);
});
await left.getByRole("button", { name: "Total on the UI thread" }).click();
await left.locator("[data-runs] li").first().waitFor({ timeout: 120000 });
const gap = await right.evaluate(() => {
  cancelAnimationFrame(window.__raf);
  let gap = 0;
  for (let i = 1; i < window.__frames.length; i++) gap = Math.max(gap, window.__frames[i] - window.__frames[i - 1]);
  return Math.round(gap);
});
console.log("left:", (await left.locator("[data-runs] li").first().innerText()).replace(/\s+/g, " "));
console.log(`right frame's longest gap between animation frames meanwhile: ${gap} ms`);
await browser.close();
