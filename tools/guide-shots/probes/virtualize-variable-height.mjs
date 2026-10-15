// Probe: Virtualize with mixed-height items (repros/virtualize-variable-height).
// Usage: node probes/virtualize-variable-height.mjs [baseUrl] [label]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5126';
const label = process.argv[3] ?? '';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 800, height: 700 } });
await page.goto(base);
await page.waitForFunction(() => window.Blazor !== undefined);
await page.waitForSelector('#thread article');
await page.waitForTimeout(1500);

const state = () => page.$eval('#thread', t => {
  const box = t.getBoundingClientRect();
  const arts = [...t.querySelectorAll('article')];
  const top = arts.find(a => a.getBoundingClientRect().bottom > box.top + 1);
  return {
    scrollTop: Math.round(t.scrollTop), scrollHeight: t.scrollHeight,
    inDom: arts.length, overflowAnchor: getComputedStyle(t).overflowAnchor,
    topMsg: top ? +top.dataset.n : null,
    topOffset: top ? Math.round(top.getBoundingClientRect().top - box.top) : null,
  };
});
const settle = () => page.waitForTimeout(400);

console.log(`=== ${label} ${base}`);
console.log('initial:', JSON.stringify(await state()));

console.log('\n--- A. scroll down in 400px steps (like a wheel), log scrollHeight and the top message');
const heights = new Set(); let backwards = 0, prevTop = 0, lines = [];
for (let i = 1; i <= 40; i++) {
  await page.$eval('#thread', (t, y) => t.scrollTop = y, i * 400);
  await settle();
  const s = await state();
  heights.add(s.scrollHeight);
  if (s.topMsg < prevTop) backwards++;
  prevTop = s.topMsg;
  if (i <= 8 || i % 8 === 0) lines.push(`  asked ${i * 400}: got scrollTop ${s.scrollTop}, scrollHeight ${s.scrollHeight}, top message #${s.topMsg}, ${s.inDom} in DOM`);
}
console.log(lines.join('\n'));
console.log(`distinct scrollHeight values over 40 steps: ${heights.size} (min ${Math.min(...heights)}, max ${Math.max(...heights)}); top message went backwards ${backwards} times`);

console.log('\n--- B. drag the thumb to 50%, then wait: which message is there, and does scrollHeight settle?');
await page.$eval('#thread', t => t.scrollTop = 0); await settle();
const h0 = (await state()).scrollHeight;
await page.$eval('#thread', t => t.scrollTop = t.scrollHeight / 2 - t.clientHeight / 2);
const snaps = [];
for (const ms of [50, 200, 600, 1200]) { await page.waitForTimeout(ms === 50 ? 50 : ms - snaps.at(-1).t); const s = await state(); snaps.push({ t: ms, ...s }); }
console.log(`  scrollHeight at top: ${h0}`);
for (const s of snaps) console.log(`  +${s.t}ms: scrollTop ${s.scrollTop}, scrollHeight ${s.scrollHeight}, top message #${s.topMsg} (middle would be ~#210)`);

console.log('\n--- C. read a message mid-thread, then press Expand all');
await page.$eval('#thread', t => t.scrollTop = 12000); await page.waitForTimeout(800);
// nudge so a long message sits just above the viewport, inside the overscan
const before = await state();
const aboveLong = await page.$eval('#thread', t => {
  const box = t.getBoundingClientRect();
  return [...t.querySelectorAll('article.clamped')].filter(a => a.getBoundingClientRect().bottom <= box.top).map(a => +a.dataset.n);
});
console.log(`  before: top message #${before.topMsg} at ${before.topOffset}px from the top of the box, scrollTop ${before.scrollTop}; clamped long messages rendered above the viewport: [${aboveLong}]`);
await page.click('#expand-all'); await page.waitForTimeout(1200);
const after = await page.$eval('#thread', (t, n) => {
  const box = t.getBoundingClientRect();
  const a = t.querySelector(`article[data-n="${n}"]`);
  const arts = [...t.querySelectorAll('article')];
  const top = arts.find(x => x.getBoundingClientRect().bottom > box.top + 1);
  return { scrollTop: Math.round(t.scrollTop), stillInDom: !!a, offset: a ? Math.round(a.getBoundingClientRect().top - box.top) : null, nowTop: top ? +top.dataset.n : null };
}, before.topMsg);
console.log(`  after Expand all: message #${before.topMsg} is ${after.stillInDom ? `at ${after.offset}px (moved ${after.offset - before.topOffset}px)` : 'no longer in the DOM'}; top message is now #${after.nowTop}; scrollTop ${after.scrollTop}`);
await page.screenshot({ path: new URL(`./virtualize-variable-height-${label || 'run'}.png`, import.meta.url).pathname });
await browser.close();
