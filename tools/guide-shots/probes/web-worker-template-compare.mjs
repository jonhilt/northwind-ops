// Presses each Total button on the guide's try page, one side at a time, as the watch steps do,
// and prints what each side's run list says (time, and clock ticks out of those expected),
// then calls a misspelled method in the worker and prints the status it shows.
// Usage: node probes/web-worker-template-compare.mjs <baseUrl> [screenshot.png]
import { chromium } from "playwright";
const [base = "http://localhost:5452", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
page.on("pageerror", e => console.log("pageerror:", e.message));
await page.goto(`${base}/guide/web-worker-template/try`, { waitUntil: "networkidle" });

const frame = side => page.frameLocator(`[data-side=${side}] iframe`);
for (const side of ["before", "after"])
  await frame(side).locator("[role=status]", { hasText: "Ready" }).waitFor({ timeout: 120000 });
console.log("ready:", await frame("before").locator("[role=status]").innerText());

async function press(side, name) {
  const f = frame(side);
  const runs = await f.locator("[data-runs] li").count();
  await f.getByRole("button", { name }).click();
  await f.locator("[data-runs] li").nth(runs).waitFor({ timeout: 120000 });
  console.log(`${side} | ${(await f.locator("[data-runs] li").nth(runs).innerText()).replace(/\s+/g, " ")}`);
}

await press("before", "Total on the UI thread");
await press("before", "Total, pausing every 1,000 lines");
await press("after", "Total in a Web Worker");
console.log("after top product:", await frame("after").locator("ol.top li").first().innerText());
await frame("after").getByRole("button", { name: "Call a misspelled method" }).click();
await frame("after").locator("[role=status]", { hasText: "Method not found" }).waitFor({ timeout: 30000 });
console.log("after misspelled |", await frame("after").locator("[role=status]").innerText());
console.log("before top product:", await frame("before").locator("ol.top li").first().innerText());
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
