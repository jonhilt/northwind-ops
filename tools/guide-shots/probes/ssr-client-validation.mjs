// Probe: .NET 10 static SSR form (repros/ssr-client-validation). Does a bad email get flagged
// before a round trip? Usage: node probes/ssr-client-validation.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5440';
const browser = await chromium.launch();
const page = await browser.newPage();
const posts = [];
page.on('request', r => { if (r.method() === 'POST') posts.push(r.url()); });
const time = () => page.textContent('#server-time');
const msgs = () => page.$$eval('.validation-message', els => els.map(e => e.textContent.trim()));

await page.goto(base);
await page.waitForTimeout(500);
const t0 = await time();
console.log('loaded; server time', t0);
console.log('input attributes:', await page.$eval('#email', e => [...e.attributes].map(a => `${a.name}="${a.value}"`).join(' ')));

await page.fill('#email', 'sales@');
await page.press('#email', 'Tab');
await page.waitForTimeout(800);
console.log('after typing "sales@" + Tab: messages', JSON.stringify(await msgs()), '| POSTs', posts.length, '| server time', await time());

await page.click('button[type=submit]');
await page.waitForTimeout(1500);
console.log('after Save with Company empty: messages', JSON.stringify(await msgs()), '| POSTs', posts.length, '| server time', await time(), '| changed:', (await time()) !== t0);
console.log('email still holds:', await page.inputValue('#email'));

await page.fill('#company', 'Acme');
await page.fill('#email', 'sales@acme.test');
await page.click('button[type=submit]');
await page.waitForTimeout(1500);
console.log('after valid Save: flash', await page.textContent('#flash').catch(() => null), '| POSTs', posts.length, '| server time', await time());
await page.screenshot({ path: new URL('./ssr-client-validation.png', import.meta.url).pathname });
await browser.close();
