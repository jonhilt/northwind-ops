// Probe for the .NET 10 wasm-env-vars-config repro: what client IConfiguration sees once WebAssembly starts.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5278';
const browser = await chromium.launch();
for (const path of ['/', '/?variable=off']) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text().split('\n')[0]); });
  await page.goto(base + path);
  await page.waitForSelector('#probe', { timeout: 30000 });
  const lines = await page.locator('#probe li').allTextContents();
  console.log(JSON.stringify({ path, lines, errors: errors.slice(0, 3) }, null, 2));
  await page.close();
}
await browser.close();
