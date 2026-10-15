// Follows the cacheview entry's watch steps on its try page: refreshes both demos, waits for the
// cache to expire, sends five visitors at once to each side, and prints what each side shows.
// Usage: node probes/cacheview-burst.mjs [baseUrl] [screenshot.png]
import { chromium } from "playwright";
const [base = "http://localhost:5440", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/cacheview/try`, { waitUntil: "networkidle" });

const frame = side => page.frameLocator(`[data-side=${side}] iframe`);
const read = async side => {
  const f = frame(side);
  const stamps = await f.locator(".stamp b").allInnerTexts();
  const tally = await f.locator("[data-tally]").innerText();
  const status = await f.locator("[data-burst-status]").innerText();
  return `page ${stamps[0]} · figures ${stamps[1]} · ${tally}${status ? " · " + status : ""}`;
};
const reload = async side => {
  const f = frame(side);
  const t0 = Date.now();
  await f.locator("[data-tally]").evaluate(e => e.ownerDocument.location.reload());
  await page.waitForTimeout(100);
  await f.locator("[data-tally]").waitFor();
  await page.waitForLoadState("networkidle");
  return Date.now() - t0;
};

for (const side of ["before", "after"]) await frame(side).locator("[data-tally]").waitFor();
console.log("loaded");
for (const side of ["before", "after"]) console.log(" ", side, await read(side));

console.log("step 1: refresh each side twice");
for (let i = 0; i < 2; i++)
  for (const side of ["before", "after"]) {
    const ms = await reload(side);
    console.log(" ", side, `~${ms} ms`, await read(side));
  }

console.log("step 2-3: wait 11 s for the cache to expire, then five visitors at once on each side");
await page.waitForTimeout(11_000);
for (const side of ["before", "after"]) {
  const f = frame(side);
  await f.locator("[data-burst]").click();
  await f.locator("[data-burst-status]", { hasText: "Last burst" }).waitFor({ timeout: 15_000 });
  console.log(" ", side, await read(side));
}

if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
