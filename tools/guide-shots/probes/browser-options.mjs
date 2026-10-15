// Probe for the .NET 10 browser-options repro: drop the circuit's connection, block rejoining,
// and read what blazor.web.js says about retries for each Blazor.start() variant.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5197';
const browser = await chromium.launch();
for (const mode of ['default', 'correct', 'typo', 'config']) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') errors.push(m.type() + ': ' + m.text().split('\n')[0]); });
  page.on('pageerror', e => errors.push('pageerror: ' + e.message.split('\n')[0]));
  await page.goto(`${base}/?mode=${mode}`);
  await page.waitForTimeout(2500);
  await page.click('#pick'); await page.click('#pick');
  await page.waitForTimeout(500);
  const picked = await page.textContent('#picked');
  const started = await page.evaluate(() => typeof Blazor !== 'undefined' && !!Blazor._internal?.forceCloseConnection);
  const out = { mode, blazorStarted: started, pickedAfterTwoClicks: picked };
  if (started) {
    // Wi-Fi "drops": every rejoin attempt (negotiate request) fails.
    await page.route('**/_blazor/negotiate**', r => r.abort());
    const t0 = Date.now();
    await page.evaluate(() => Blazor._internal.forceCloseConnection());
    const samples = [];
    for (let i = 0; i < 8; i++) {
      await page.waitForTimeout(1000);
      samples.push({
        s: Math.round((Date.now() - t0) / 1000),
        attempt: await page.textContent('#components-reconnect-current-attempt'),
        of: await page.textContent('#components-reconnect-max-retries'),
        dialog: (await page.locator('#components-reconnect-modal p:visible').allTextContents()).map(s => s.trim().replace(/\s+/g, ' ')),
      });
    }
    out.samples = samples;
  }
  out.consoleErrors = errors.slice(0, 4);
  console.log(JSON.stringify(out));
  await page.close();
}
await browser.close();
