// Drives both sides of the label-and-input-ids guide the way its watch steps say: click
// "Company name" on each, then save each form empty, printing what each side's panel and
// messages show. Usage: node probes/label-and-input-ids-sides.mjs <baseUrl> [screenshot.png]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5400';
const shot = process.argv[3];
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
await page.goto(`${base}/guide/label-and-input-ids/try`);
await page.waitForLoadState('networkidle');

const frames = {
  left: page.frameLocator('iframe[src="demo/labels/suppliers/new/net10"]'),
  right: page.frameLocator('iframe[src="demo/labels/suppliers/new"]'),
};
const readout = f => f.locator('[data-label-readout]').innerText();

for (const [side, f] of Object.entries(frames)) {
  await f.locator('h1').waitFor();
  await f.locator('label', { hasText: 'Company name' }).click({ position: { x: 5, y: 5 } });
  await page.waitForTimeout(150);
  const focused = await f.locator(':focus').evaluateAll(els => els.map(e => e.id || e.name).join() || 'nothing');
  console.log(`--- ${side}: after clicking "Company name", focus: ${focused}`);
  console.log(await readout(f));
}
for (const [side, f] of Object.entries(frames)) {
  await f.locator('button[type=submit]').click();
  await f.locator('.validation-message').first().waitFor();
  await page.waitForTimeout(300);
  const labels = await f.locator('label').evaluateAll(ls => ls.map(l =>
    [...l.childNodes].filter(n => n.nodeType === 3).map(n => n.textContent).join(' ').trim()));
  const messages = await f.locator('.validation-message').allInnerTexts();
  console.log(`--- ${side}: after saving empty, labels: ${JSON.stringify(labels)}, messages: ${JSON.stringify(messages)}`);
  console.log(await readout(f));
}
// Open the answer too, so the after-shot shows the guess being checked.
await page.getByText('Check your guess').click();
await page.waitForTimeout(200);
console.log('--- answer:', (await page.locator('details:has-text("Check your guess")').innerText()).replace(/\s+/g, ' '));
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
