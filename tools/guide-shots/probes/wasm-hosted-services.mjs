// Probe for the .NET 10 wasm-hosted-services repro: does a service registered with AddHostedService ever start?
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5345';
const browser = await chromium.launch();
const page = await browser.newPage();
const logs = [];
page.on('console', m => logs.push(`${m.type()}: ${m.text().split('\n')[0]}`));
const t0 = Date.now();
const snap = async (label) => console.log(JSON.stringify({
  at: `${((Date.now() - t0) / 1000).toFixed(1)}s`, label, url: page.url().replace(base, ''),
  hosted: await page.locator('#hosted').textContent(),
  manual: await page.locator('#manual').textContent(),
  component: await page.locator('#component').count() ? await page.locator('#component').textContent() : '(not on this page)',
}));
await page.goto(base + '/');
await page.locator('#hosted').waitFor();
await page.waitForTimeout(5000);
await snap('after 5s on Orders');
await page.click('#customers');
await page.waitForTimeout(3000);
await snap('3s after clicking Customers');
await page.click('#orders');
await page.waitForTimeout(1500);
await snap('1.5s after clicking back to Orders');
console.log('console:', JSON.stringify(logs.filter(l => !l.includes('Debugging hotkey')), null, 1));
await browser.close();
