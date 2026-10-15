// Screenshots each Field Guide entry the way a learner meets it: the problem,
// the demo, and the code, at a laptop-sized window.
// Usage: node shoot.mjs <outDir> [baseUrl] [id ...]   (no ids: every entry on /guide)
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";

const [outDir, base = "https://lab.practicaldotnet.io", ...only] = process.argv.slice(2);
if (!outDir) throw new Error("usage: node shoot.mjs <outDir> [baseUrl] [id ...]");

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });

let ids = only;
if (!ids.length) {
  await page.goto(`${base}/guide`, { waitUntil: "networkidle" });
  ids = await page.$$eval('a[href*="guide/"]', as => [...new Set(as
    .map(a => new URL(a.href).pathname.match(/^\/guide\/([^/]+)$/)?.[1])
    .filter(Boolean))]);
}

for (const id of ids) {
  const dir = `${outDir}/${id}`;
  await mkdir(dir, { recursive: true });
  for (const [step, path] of [["1-problem", ""], ["2-try", "/try"], ["3-code", "/code"]]) {
    await page.goto(`${base}/guide/${id}${path}`, { waitUntil: "networkidle" });
    await page.waitForTimeout(1500); // let an interactive demo in the frame start
    await page.screenshot({ path: `${dir}/${step}.png`, fullPage: true });
  }
  console.log(id);
}
await browser.close();
