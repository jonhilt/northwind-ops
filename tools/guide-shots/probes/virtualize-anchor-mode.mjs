// Probe: .NET 10 Virtualize has no AnchorMode (repros/virtualize-anchor-mode).
// Usage: node probes/virtualize-anchor-mode.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5549';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 900, height: 700 } });
const ready = async () => { await page.waitForFunction(() => window.Blazor !== undefined); await page.waitForSelector('#feed .msg'); await page.waitForTimeout(1000); };
const m = () => page.$eval('#feed', el => {
  const rows = [...el.querySelectorAll('.msg')];
  const box = el.getBoundingClientRect();
  const visible = rows.filter(r => { const b = r.getBoundingClientRect(); return b.bottom > box.top + 1 && b.top < box.bottom - 1; }).map(r => +r.dataset.n);
  return { gapBelow: Math.round(el.scrollHeight - el.scrollTop - el.clientHeight), rowsInDom: rows.length, lastVisible: Math.max(...visible) };
});
const settleBottom = async () => { for (let i = 0; i < 15; i++) { await page.$eval('#feed', el => el.scrollTop = el.scrollHeight); await page.waitForTimeout(150); } };
const add = async () => { await page.click('#add'); await page.waitForTimeout(600); };

console.log('--- A. "/" plain Virtualize, 50px rows, just add the item (what the .NET 11 demo does)');
await page.goto(base); await ready();
await settleBottom();
console.log('at bottom:', await m(), '|', await page.textContent('#count'));
for (let i = 0; i < 3; i++) { await add(); console.log(`after add #${i + 1}:`, await m(), '|', await page.textContent('#count')); }

console.log('\n--- B. "/js-workaround" isAtBottom + scrollToBottom over JS interop, mixed row heights, 300 items');
await page.goto(base + '/js-workaround'); await ready();
await settleBottom();
console.log('at bottom:', await m(), '|', await page.textContent('#count'));
for (let i = 0; i < 6; i++) { await add(); console.log(`after add #${i + 1}:`, await m(), '|', await page.textContent('#count')); }
await page.screenshot({ path: new URL('./virtualize-anchor-mode.png', import.meta.url).pathname });
await browser.close();
