// Screenshots a walkthrough the way a learner meets it: consecutive viewport-sized slices, top to bottom.
// Usage: node learn-shots.mjs <outDir> <baseUrl> <id ...>   (writes <outDir>/<id>/00.png, 01.png, ...)
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";

const [outDir, base, ...ids] = process.argv.slice(2);
if (!outDir || !base || !ids.length) throw new Error("usage: node learn-shots.mjs <outDir> <baseUrl> <id ...>");

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
for (const id of ids) {
  const dir = `${outDir}/${id}`;
  await mkdir(dir, { recursive: true });
  await page.goto(`${base}/learn/${id}`, { waitUntil: "networkidle" });
  await page.waitForTimeout(2000); // let the demos in the figures start
  const height = await page.evaluate(() => document.body.scrollHeight);
  for (let y = 0, i = 0; y < height; y += 760, i++) {
    await page.evaluate(y => window.scrollTo(0, y), y);
    await page.waitForTimeout(400);
    await page.screenshot({ path: `${dir}/${String(i).padStart(2, "0")}.png` });
  }
  console.log(id);
}
await browser.close();
