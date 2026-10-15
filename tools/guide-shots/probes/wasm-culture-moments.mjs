// Reads the culture demo's "moments" table once WebAssembly has started, in a browser of the given language.
import { chromium } from "playwright";
const [base = "http://localhost:5321", locale = "en-US"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await (await browser.newContext({ locale })).newPage();
await page.goto(`${base}/demo/wasm-culture/invoice?culture=de-DE`);
await page.waitForFunction(() => document.querySelector(".runtime-badge")?.textContent.includes("WebAssembly"), null, { timeout: 60000 });
await page.waitForTimeout(500);
console.log(locale, await page.$$eval("[data-moments] tbody tr", rows => rows.map(r => r.innerText.replace(/\s+/g, " "))));
await browser.close();
