// .NET 10 repro probe: does QuickGrid sort/page survive in the URL? Run against the repro on :5522.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5522';
const browser = await chromium.launch();
const page = await browser.newPage();
const state = async (label) => {
  const first = await page.locator('tbody tr').first().innerText();
  const pg = await page.locator('.pagination-text, nav').first().innerText().catch(() => '?');
  console.log(`${label.padEnd(34)} url=${page.url().replace(base, '')}  firstRow=${first.replace(/\s+/g, ' ')}  pager="${pg.replace(/\s+/g, ' ').trim()}"`);
};
for (const path of ['/static', '/interactive']) {
  console.log(`\n=== ${path}`);
  let requests = 0; page.on('request', () => requests++);
  let ws = false; page.on('websocket', () => ws = true);
  await page.goto(base + path, { waitUntil: 'networkidle' });
  await page.waitForTimeout(800);
  await state('loaded');
  await page.getByRole('button', { name: 'Price' }).click(); await page.waitForTimeout(400);
  await page.getByRole('button', { name: 'Price' }).click(); await page.waitForTimeout(400);
  await state('clicked Price twice');
  await page.getByRole('button', { name: 'Go to next page' }).click(); await page.waitForTimeout(400);
  await state('clicked next page');
  await page.reload({ waitUntil: 'networkidle' }); await page.waitForTimeout(800);
  await state('after refresh');
  await page.goto(base + path + '?sort=Price&direction=desc&page=2', { waitUntil: 'networkidle' }); await page.waitForTimeout(800);
  await state('opened ?sort=Price&direction=desc&page=2');
  console.log(`websocket opened: ${ws}`);
  page.removeAllListeners('request'); page.removeAllListeners('websocket');
}
await browser.close();
