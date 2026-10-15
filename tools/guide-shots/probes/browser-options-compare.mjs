// Probe for the browser-options guide entry, on the .NET 11 app: drives the .NET 10 page (both ways)
// and the BrowserOptions page the way "See it work" says, and prints what each shows.
// Usage: node probes/browser-options-compare.mjs [baseUrl] [screenshot.png]
// With a screenshot path, also saves <name>-config.png: the left side switched to Settings from config.
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5424';
const shot = process.argv[3];
const browser = await chromium.launch();

async function side(url, drop) {
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(e.message.split('\n')[0]));
  page.on('console', m => { if (m.type() === 'error' && /Content Security|Refused/.test(m.text())) errors.push(m.text().split('\n')[0]); });
  await page.goto(base + url);
  await page.waitForTimeout(3000);
  const buttons = page.locator('table.picks button');
  await buttons.nth(0).click({ timeout: 2000 }).catch(() => {});
  await buttons.nth(0).click({ timeout: 2000 }).catch(() => {});
  await page.waitForTimeout(500);
  const out = {
    url,
    picked: (await page.textContent('.count')).trim(),
    sent: (await page.textContent('[data-sent-code]')).trim(),
    started: (await page.textContent('[data-sent-started]')).trim(),
    errors,
  };
  if (drop) {
    await page.click('.dropout .drop');
    const samples = [];
    for (let s = 1; s <= 13; s++) {
      await page.waitForTimeout(1000);
      const attempt = await page.textContent('#components-reconnect-current-attempt');
      const of = await page.textContent('#components-reconnect-max-retries');
      const dialog = (await page.locator('#components-reconnect-modal p:visible').allTextContents()).map(t => t.trim().replace(/\s+/g, ' ')).pop() ?? '(closed)';
      const gaps = (await page.textContent('[data-gaps]')).trim();
      samples.push(`${s}s: ${attempt} of ${of} · ${dialog} · ${gaps}`);
    }
    out.samples = samples;
    out.pickedAfter = (await page.textContent('.count')).trim();
  }
  await page.close();
  return out;
}

for (const [url, drop] of [['/demo/browser-options/pick-list/net10', true], ['/demo/browser-options/pick-list/net10?from=config', false], ['/demo/browser-options/pick-list', true]]) {
  console.log(JSON.stringify(await side(url, drop), null, 1));
}

if (shot) {
  // The guide's try page, after picking a line on both sides and dropping both Wi-Fis for a few seconds.
  const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });
  await page.goto(`${base}/guide/browser-options/try`);
  await page.waitForTimeout(3500);
  for (const frame of page.frames().filter(f => f.url().includes('/demo/'))) {
    await frame.locator('table.picks button').first().click();
  }
  for (const frame of page.frames().filter(f => f.url().includes('/demo/'))) {
    await frame.locator('.dropout .drop').click();
  }
  // Long enough for the left side's first 5-second wait to be measured.
  await page.waitForTimeout(7500);
  await page.screenshot({ path: shot, fullPage: true });

  // The second .NET 10 way: the left side switched to Settings from config, and Pick pressed on both.
  await page.goto(`${base}/guide/browser-options/try`);
  await page.waitForTimeout(2500);
  const left = () => page.frames().find(f => f.url().includes('/net10'));
  await left().locator('.ways a', { hasText: 'Settings from config' }).click();
  await page.waitForTimeout(3500);
  for (const frame of page.frames().filter(f => f.url().includes('/demo/'))) {
    await frame.locator('table.picks button').first().click({ timeout: 2000 }).catch(() => {});
  }
  await page.waitForTimeout(800);
  for (const frame of page.frames().filter(f => f.url().includes('/demo/'))) {
    console.log(frame.url().replace(base, ''), '·', (await frame.textContent('.count')).trim(), '·',
      (await frame.textContent('[data-sent-started]')).trim());
  }
  await page.screenshot({ path: shot.replace(/\.png$/, '-config.png'), fullPage: true });
}
await browser.close();
