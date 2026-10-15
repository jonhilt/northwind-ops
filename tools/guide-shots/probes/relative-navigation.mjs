// Drives the .NET 10 relative-navigation repro in a real browser (enhanced navigation on).
// Usage: node probes/relative-navigation.mjs [base]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5277';
const browser = await chromium.launch();
const page = await browser.newPage();
const show = async (what) => console.log(`${what}\n   -> ${page.url().replace(base, '')} | h1: ${(await page.textContent('h1')).trim()}`);

await page.goto(base + '/');
await page.click('text=11077');
await page.waitForLoadState('networkidle');
await show('Click order 11077 on / (page then calls NavigateTo("summary"))');

await page.goto(base + '/orders/11077/summary');
for (const nav of ['naive', 'fullpath', 'lookalike']) {
  const hrefs = await page.$$eval(`#${nav} a`, as => as.map(a => `${a.getAttribute('href')} => ${a.href}${a.classList.contains('active') ? ' [active]' : ''}`));
  console.log(`#${nav} on /orders/11077/summary:\n   ${hrefs.join('\n   ')}`);
}
for (const nav of ['naive', 'fullpath', 'lookalike']) {
  await page.goto(base + '/orders/11077/summary');
  await page.click(`#${nav} >> text=Lines`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(300);
  await show(`Click #${nav} "Lines" on /orders/11077/summary`);
}
await browser.close();
