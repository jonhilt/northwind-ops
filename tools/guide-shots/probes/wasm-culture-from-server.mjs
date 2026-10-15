// Probe for the .NET 10 wasm-culture-from-server repro: load ?culture=de-DE in browsers with
// different languages and record what each invoice line says when prerendered vs after WebAssembly starts.
import { chromium } from 'playwright';
const base = process.env.BASE ?? 'http://localhost:5262';
const browser = await chromium.launch();
const read = page => page.evaluate(() => [...document.querySelectorAll('.invoice')].map(p => p.textContent.replace(/\s+/g, ' ').trim()));
for (const [locale, query] of [['en-US', '?culture=de-DE'], ['fr-FR', '?culture=de-DE'], ['en-US', '?culture=de-DE&fix=start']]) {
  const ctx = await browser.newContext({ locale });
  const page = await ctx.newPage();
  // Snapshot every text change so the flip is visible.
  await page.addInitScript(() => {
    window.__seen = [];
    new MutationObserver(() => {
      const t = [...document.querySelectorAll('.invoice')].map(p => p.textContent.replace(/\s+/g, ' ').trim()).join(' | ');
      if (t && window.__seen.at(-1) !== t) window.__seen.push(t);
    }).observe(document, { subtree: true, childList: true, characterData: true });
  });
  await page.goto(base + '/' + query, { waitUntil: 'domcontentloaded' });
  const prerendered = await read(page);
  await page.waitForFunction(() => document.querySelectorAll('.invoice b')[0]?.textContent === 'WebAssembly', null, { timeout: 30000 });
  await page.waitForTimeout(500);
  console.log(JSON.stringify({ browserLocale: locale, url: query, prerendered, afterWebAssembly: await read(page) }, null, 1));
  await ctx.close();
}
await browser.close();
