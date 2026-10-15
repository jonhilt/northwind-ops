// Drives the .NET 10 repro: the static page with an interactive card fails; the whole-page-interactive workaround works.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5354';
const browser = await chromium.launch();
const page = await browser.newPage();
const r1 = await page.goto(base + '/warehouse');
console.log('/warehouse status', r1.status(), '|', (await page.textContent('body')).replace(/\s+/g,' ').slice(0, 220));
const r2 = await page.goto(base + '/warehouse-interactive');
console.log('/warehouse-interactive status', r2.status());
await page.waitForSelector('#mode:text("Interactive")', { timeout: 10000 });
const stamp1 = await page.textContent('b');
await page.click('#toggle'); await page.waitForTimeout(300);
console.log('after fold, table visible:', await page.isVisible('table'));
await page.click('#toggle'); await page.waitForTimeout(300);
console.log('after reopen, table visible:', await page.isVisible('table'), 'stamp', stamp1, '->', await page.textContent('b'));
const ws = []; page.on('websocket', w => ws.push(w.url()));
await browser.close();
