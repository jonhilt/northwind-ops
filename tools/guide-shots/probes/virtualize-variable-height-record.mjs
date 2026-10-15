// Records the variable-height list scrolling and expanding, with scroll-readout.js on screen.
// Usage: node probes/virtualize-variable-height-record.mjs <url> <videoDir> [readoutScript] [screenshot]
import { chromium } from "playwright";
const [url, videoDir, readout = "../../src/NorthwindOps/wwwroot/js/scroll-readout.js"] = process.argv.slice(2);
const size = { width: 560, height: 620 };
const browser = await chromium.launch();
const context = await browser.newContext({ viewport: size, recordVideo: { dir: videoDir, size } });
const page = await context.newPage();
await page.goto(url, { waitUntil: "networkidle" });
await page.waitForSelector(".thread article");
await page.waitForTimeout(1500);
if (!(await page.$("[data-scroll-readout]"))) await page.addScriptTag({ path: readout });
await page.waitForTimeout(800);
const read = () => page.$eval("[data-scroll-readout]", e => e.textContent);
for (let i = 1; i <= 24; i++) {
  await page.$eval(".thread", (t, y) => t.scrollTop = y, i * 400);
  await page.waitForTimeout(350);
}
console.log("after scrolling:", await read());
await page.$eval(".thread", t => t.scrollTop = t.scrollHeight / 2);
await page.waitForTimeout(1200);
await page.getByRole("button", { name: "Expand all" }).click();
await page.waitForTimeout(2500);
console.log("after expand:   ", await read());
if (process.argv[5]) await page.screenshot({ path: process.argv[5] });
await context.close();
await browser.close();
