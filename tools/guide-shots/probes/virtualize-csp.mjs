// Probe: Virtualize under style-src 'self' (repros/virtualize-csp).
// Usage: node probes/virtualize-csp.mjs [net10Url] [net11Url]
import { chromium } from 'playwright';
const urls = [process.argv[2] ?? 'http://localhost:5674', process.argv[3] ?? 'http://localhost:5675'];
const browser = await chromium.launch();
for (const base of urls) {
  for (const q of ['/', '/?csp=off']) {
    const page = await browser.newPage();
    const csp = [];
    page.on('console', m => { if (m.type() === 'error' && /Content Security Policy/.test(m.text())) csp.push(m.text()); });
    await page.goto(base + q);
    await page.waitForFunction(() => window.Blazor !== undefined);
    await page.waitForTimeout(1500);
    const h1 = await page.textContent('h1');
    const spacers = () => page.$$eval('#list > div[aria-hidden]', ds => ds.map(d =>
      `style="${d.getAttribute('style')}" data-blazor-virtualize-reserved-height=${d.getAttribute('data-blazor-virtualize-reserved-height')} -> ${d.getBoundingClientRect().height}px`));
    const scroll = () => page.$eval('#list', l => `scrollHeight ${l.scrollHeight}px`);
    const rows = () => page.$$eval('#list .row', rs => `${rs.length} rows rendered, ${rs[0]?.textContent} .. ${rs.at(-1)?.textContent}`);
    console.log(`\n=== ${base}${q}  ${h1}`);
    console.log('CSP header:', (await page.evaluate(async () => (await fetch(location.href)).headers.get('content-security-policy'))) ?? 'none');
    console.log('start   :', await scroll(), '|', await rows());
    console.log('spacers :', (await spacers()).join(' | '));
    for (let i = 0; i < 30; i++) { await page.$eval('#list', l => l.scrollTop = l.scrollHeight); await page.waitForTimeout(150); }
    await page.waitForTimeout(800);
    console.log('end     :', await scroll(), '|', await rows());
    console.log('spacers :', (await spacers()).join(' | '));
    console.log(`CSP console errors: ${csp.length}${csp.length ? '  first: ' + csp[0].slice(0, 220) : ''}`);
    if (q === '/') await page.screenshot({ path: new URL(`./virtualize-csp-${base.endsWith('5674') ? 'net10' : 'net11'}.png`, import.meta.url).pathname, clip: { x: 0, y: 0, width: 400, height: 520 } });
    await page.close();
  }
}
await browser.close();
