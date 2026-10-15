// Probe: .NET 10 workarounds for an async "is this code taken?" check (repros/async-form-validation).
// Usage: node probes/async-form-validation.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5135';
const browser = await chromium.launch();
const page = await browser.newPage();
const msgs = () => page.$$eval('.validation-message', els => els.map(e => e.textContent.trim()));
const ready = async () => { await page.waitForFunction(() => window.Blazor !== undefined); await page.waitForTimeout(1200); };

console.log('--- A. async void OnFieldChanged workaround: type a taken code, press Save straight away');
await page.goto(base); await ready();
console.log('codes in db before:', await page.textContent('#codes'));
await page.fill('#code', 'EXOT-01');
await page.press('#code', 'Tab');
await page.waitForTimeout(300);
console.log('+300ms after Tab: messages', JSON.stringify(await msgs()), '| save disabled?', await page.$eval('#save', b => b.disabled), '| body mentions checking?', /check/i.test(await page.textContent('form')));
await page.click('#save');
await page.waitForTimeout(400);
console.log('right after Save: messages', JSON.stringify(await msgs()), '| codes in db:', await page.textContent('#codes'));
await page.waitForTimeout(1500);
console.log('1.5s later: messages', JSON.stringify(await msgs()), '| codes in db:', await page.textContent('#codes'));
console.log('log:\n  ' + (await page.$$eval('#log li', l => l.map(x => x.textContent))).join('\n  '));

console.log('\n--- B. same form, change the code twice quickly (no cancellation)');
await page.goto(base); await ready();
await page.fill('#code', 'EXOT-01'); await page.press('#code', 'Tab');
await page.waitForTimeout(200);
await page.fill('#code', 'ZZZZ-99'); await page.press('#code', 'Tab');
await page.waitForTimeout(2200);
console.log('field holds', await page.inputValue('#code'), '| messages', JSON.stringify(await msgs()));
console.log('log:\n  ' + (await page.$$eval('#log li', l => l.map(x => x.textContent))).join('\n  '));

console.log('\n--- C. sync ValidationAttribute that blocks on the lookup');
await page.goto(base + '/sync-attribute'); await ready();
await page.fill('#code', 'EXOT-01');
const t0 = Date.now();
await page.press('#code', 'Tab');
await page.waitForTimeout(300);
console.log('+300ms after Tab: messages', JSON.stringify(await msgs()), '| any "checking" text?', /check/i.test(await page.textContent('form')));
const appeared = await page.waitForFunction(() => document.querySelector('.validation-message'), null, { timeout: 20000 }).then(() => true, () => false);
if (!appeared) { console.log(`no message after ${Date.now() - t0} ms; circuit still answering a click?`); await page.fill('#code', 'ABCD-12'); await page.press('#code', 'Tab'); await page.waitForTimeout(2000); console.log('after editing again: messages', JSON.stringify(await msgs())); await browser.close(); process.exit(0); }
console.log(`message appeared after ${Date.now() - t0} ms:`, JSON.stringify(await msgs()));
await browser.close();
