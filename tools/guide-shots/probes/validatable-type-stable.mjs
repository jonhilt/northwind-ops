// Probe: on the live static-SSR demo, does typing "sales@" + Tab flag anything before submit? Then what does Save flag?
import { chromium } from 'playwright';
const url = process.argv[2] ?? 'https://lab.practicaldotnet.io/demo/nested-validation/suppliers/new';
const b = await chromium.launch(); const p = await b.newPage();
const posts = []; p.on('request', r => { if (r.method() === 'POST') posts.push(r.url()); });
await p.goto(url, { waitUntil: 'networkidle' });
const msgs = () => p.$$eval('.validation-message', els => els.map(e => e.textContent.trim()));
await p.fill('input[name="Input.Contacts[0].Email"]', 'sales@'); await p.keyboard.press('Tab'); await p.waitForTimeout(800);
console.log('after Tab:', JSON.stringify(await msgs()), 'emailClass=', await p.getAttribute('input[name="Input.Contacts[0].Email"]', 'class'), 'posts=', posts.length);
await p.click('button[type=submit]'); await p.waitForTimeout(2500);
console.log('after Save:', JSON.stringify(await msgs()), 'posts=', posts.length);
await b.close();
