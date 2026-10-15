// Drives the guide's "See it work" for wasm-env-vars-config the way its watch steps say:
// reads the recorded .NET 10 side and the live .NET 11 side, then clicks "Build's own settings".
// Usage: node wasm-env-vars-config-guide.mjs [baseUrl] [screenshotPath]
import { chromium } from "playwright";
const [base = "http://localhost:5436", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
await page.goto(`${base}/guide/wasm-env-vars-config/try`);

const frames = () => page.frames().filter(f => f.url().includes("/demo/env-config/"));
const before = () => frames().find(f => f.url().includes("/net10"));
const after = () => frames().find(f => !f.url().includes("/net10"));
const read = frame => frame.evaluate(() => ({
  calling: document.querySelector("p code")?.textContent,
  error: document.querySelector(".error")?.textContent.replace(/\s+/g, " ").trim(),
  orders: document.querySelectorAll(".quickgrid tbody tr, .quickgrid tr:has(td)").length,
  sources: [...document.querySelectorAll("[data-sources] tr")].map(r => r.innerText.replace(/\s+/g, " | ")),
  note: document.querySelector(".note")?.textContent.replace(/\s+/g, " ").trim(),
}));

await page.waitForFunction(() => document.querySelectorAll("iframe").length >= 2);
await page.waitForTimeout(500);
const waitWasm = async () => {
  for (let i = 0; i < 120; i++) {
    const f = after();
    if (f && await f.evaluate(() => document.querySelector(".runtime-badge")?.textContent.includes("WebAssembly")
      && !!document.querySelector("[data-sources] td")).catch(() => false)) return f;
    await page.waitForTimeout(500);
  }
  throw new Error("WebAssembly never started");
};

console.log("LEFT (.NET 10, recorded):", JSON.stringify(await read(before()), null, 1));
await waitWasm();
await page.waitForTimeout(1500);
console.log("RIGHT (.NET 11, live):", JSON.stringify(await read(after()), null, 1));

await after().click("text=Build's own settings");
await page.waitForTimeout(1000);
await waitWasm();
await page.waitForTimeout(2000);
console.log("RIGHT after Build's own settings:", after().url(), JSON.stringify(await read(after()), null, 1));
if (shot) await page.screenshot({ path: shot, fullPage: false });
await browser.close();
