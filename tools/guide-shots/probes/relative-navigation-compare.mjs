// Drives both sides of the relative-navigation guide the way its watch steps say, printing
// each frame's address, "You are at" line, active tab and heading. With paths, screenshots the
// try page after clicking both Lines tabs, and again after the right side's redirect (step 4).
// Usage: node probes/relative-navigation-compare.mjs [base] [shot.png] [redirect-shot.png]
import { chromium } from "playwright";
const [base = "http://localhost:5412", shot, redirectShot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/relative-navigation/try`, { waitUntil: "networkidle" });

const sideFrame = async (side) => (await page.locator(`[data-side=${side}] iframe`).elementHandle()).contentFrame();
const report = async (side, what) => {
  const f = await sideFrame(side);
  const address = await page.locator(`[data-side=${side}] [data-demo-address]`).innerText();
  const active = await f.$$eval("nav.tabs a.active", (as) => as.map((a) => a.textContent.trim()));
  const hrefs = await f.$$eval("nav.tabs a", (as) => as.map((a) => new URL(a.href).pathname));
  const heading = await f.$eval("h1, h3", (h) => h.textContent.trim()).catch(() => "(none)");
  const youAreAt = await f.$eval("p.address", (p) => p.textContent.trim()).catch(() => "(none)");
  const body = (await f.$$eval(".demo-body p:not(.address)", (ps) => ps.slice(0, 2).map((p) => p.textContent.trim()))).join(" | ");
  console.log(`${side} ${what}\n   address: ${address}\n   readout: ${youAreAt}\n   heading: ${heading}\n   active tab: ${active.join(", ") || "none"}\n   tab hrefs: ${hrefs.join(" ")}\n   shows: ${body}`);
};
const click = async (side, text) => {
  const f = await sideFrame(side);
  await f.click(`text=${text}`);
  await page.waitForTimeout(1200);
};

await report("before", "on load");
await report("after", "on load");
await click("before", "Lines");
await report("before", "after clicking Lines");
await click("after", "Lines");
await report("after", "after clicking Lines");
if (shot) await page.screenshot({ path: shot, fullPage: true });
await click("after", "← Orders");
const f = await sideFrame("after");
const other = await f.$$eval("tbody a", (as) => as[1].textContent.trim());
await click("after", other);
await report("after", `after ← Orders, then order ${other}`);
if (redirectShot) await page.screenshot({ path: redirectShot, fullPage: true });
await browser.close();
