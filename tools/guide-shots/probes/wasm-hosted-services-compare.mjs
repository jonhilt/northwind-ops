// Drives both of the hosted-services entry's bells the way "See it work" says, and reads each one:
// ~10 s on Orders, ~6 s on Customers (no bell there), then back to Orders.
// Usage: node probes/wasm-hosted-services-compare.mjs [baseUrl] [screenshot.png]
import { chromium } from "playwright";
const [base = "http://localhost:5428", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
const errors = [];
page.on("console", m => { if (m.type() === "error") errors.push(m.text()); });
await page.goto(`${base}/guide/wasm-hosted-services/try`, { waitUntil: "networkidle" });
const sides = ["before", "after"];
const side = s => page.frameLocator(`[data-side=${s}] iframe`);
const go = async tab => {
  for (const s of sides) await side(s).locator(".tabs a", { hasText: tab }).click();
  for (const s of sides) await side(s).locator("h1", { hasText: tab }).waitFor();
};
const read = async label => {
  for (const s of sides) {
    const f = side(s);
    const bell = await f.locator("[data-checks]").count()
      ? `${(await f.locator("[data-checks]").innerText()).replace(/\s+/g, " ").trim()}`
        + ` · updates ${await f.locator(".bell .count").innerText()}`
        + ` · ${await f.locator(".bell .latest").count() ? await f.locator(".bell .latest").innerText() : "(no latest)"}`
      : "(no bell on this page)";
    console.log(`${label.padEnd(24)} ${s.padEnd(6)} ${bell}`);
  }
};
for (const s of sides) await side(s).locator("[data-checks]").waitFor();
await page.waitForTimeout(10000);
await read("1. ~10 s on Orders");
await go("Customers");
await page.waitForTimeout(6000);
await read("2. ~6 s on Customers");
await go("Orders");
for (const s of sides) await side(s).locator("[data-checks]").waitFor();
await page.waitForTimeout(500);
await read("3. back on Orders");
await page.waitForTimeout(3000);
await read("3. 3 s later");
const moments = await side("before").locator("[data-moments]").innerText();
console.log("   recording:", moments.replace(/\t/g, " | ").split("\n").join(" / "));
console.log("console errors:", errors.length ? errors : "none");
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
