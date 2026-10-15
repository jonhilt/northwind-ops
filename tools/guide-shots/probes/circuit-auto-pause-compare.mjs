// Drives the auto-pause guide's two demos the way "See it work" says, and prints what each side shows.
// Usage: node probes/circuit-auto-pause-compare.mjs [base] [screenshot.png]
// Headless Chromium never hides a tab, so hiding overrides document.visibilityState in both frames
// and fires visibilitychange, which is what a real tab switch does as far as the page's scripts know.
import { chromium } from "playwright";
const [base = "http://localhost:5420", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.goto(`${base}/guide/circuit-auto-pause/try`, { waitUntil: "networkidle" });
const sides = ["before", "after"];
const frame = side => page.frameLocator(`[data-side=${side}] iframe`);
const setHidden = hidden => Promise.all(sides.map(async side => {
  const f = await (await page.locator(`[data-side=${side}] iframe`).elementHandle()).contentFrame();
  await f.evaluate(h => {
    Object.defineProperty(document, "visibilityState", { configurable: true, get: () => h ? "hidden" : "visible" });
    Object.defineProperty(document, "hidden", { configurable: true, get: () => h });
    document.dispatchEvent(new Event("visibilitychange"));
  }, hidden);
}));
const report = async label => {
  for (const side of sides) {
    const f = frame(side);
    const panel = (await f.locator(".circuit-panel dd").first().innerText()).replace(/\s+/g, " ");
    const counts = (await f.locator(".count b").allInnerTexts()).join(",");
    console.log(`${label} ${side.padEnd(6)} | ${panel} | counts ${counts} | ${await f.locator("[data-posted]").innerText()} | ${await f.locator("[data-ledger]").innerText()}`);
  }
};

for (const side of sides) await frame(side).locator(".post button").waitFor();
await page.waitForTimeout(1500); // both circuits start
// Step 1: count a few products on each side, post, and hide the tab at once.
for (const side of sides) {
  for (const row of [0, 0, 1]) await frame(side).locator(".count").nth(row).locator("button").last().click();
  await frame(side).locator(".post button").click();
}
await report("1 posted          ");
await setHidden(true);
await page.waitForTimeout(30_000);
await setHidden(false);
await page.waitForTimeout(3000);
// Steps 2-4: what each side shows on coming back.
await report("2 back after 30 s ");
if (shot) await page.screenshot({ path: shot, fullPage: true });
// Step 3: post again on the left, and wait for the warehouse.
await frame("before").locator(".post button").click();
await page.waitForTimeout(21_000);
await report("3 left posts again");
await browser.close();
