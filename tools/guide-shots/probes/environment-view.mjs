// Probe for the .NET 10 environment-view repro: what happens in the browser once WebAssembly takes over.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5871';
const browser = await chromium.launch();
for (const path of ['/host-env', '/wasm-env']) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text().split('\n')[0]); });
  page.on('pageerror', e => errors.push('pageerror: ' + e.message.split('\n')[0]));
  const res = await page.goto(base + path);
  await page.waitForTimeout(8000);
  const banner = await page.locator('.env-banner').allTextContents();
  const errorUi = await page.locator('#blazor-error-ui').isVisible();
  console.log(JSON.stringify({ path, status: res.status(), bannerAfterWasm: banner, errorUiVisible: errorUi, errors: errors.slice(0, 3) }, null, 2));
  await page.close();
}
await browser.close();
