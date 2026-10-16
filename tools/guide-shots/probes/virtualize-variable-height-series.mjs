// Probe: the numbers behind the variable-height walkthrough's charts. Scrolls the repro's thread in 40 steps of
// 400px and records the list's full height (scrollHeight) at each, then presses Expand all while reading a message
// mid-thread and records how far that message moved. Prints JSON.
// Usage: node probes/virtualize-variable-height-series.mjs <baseUrl>
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5126';
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 800, height: 700 } });
await page.goto(base);
await page.waitForFunction(() => window.Blazor !== undefined);
await page.waitForSelector('#thread article');
await page.waitForTimeout(1500);

const height = () => page.$eval('#thread', t => t.scrollHeight);
const heights = [await height()];
for (let i = 1; i <= 40; i++) {
  await page.$eval('#thread', (t, y) => t.scrollTop = y, i * 400);
  await page.waitForTimeout(400);
  heights.push(await height());
}

await page.$eval('#thread', t => t.scrollTop = 12000);
await page.waitForTimeout(800);
const reading = () => page.$eval('#thread', t => {
  const box = t.getBoundingClientRect();
  const top = [...t.querySelectorAll('article')].find(a => a.getBoundingClientRect().bottom > box.top + 1);
  return { n: +top.dataset.n, offset: Math.round(top.getBoundingClientRect().top - box.top) };
});
const before = await reading();
await page.click('#expand-all');
await page.waitForTimeout(1200);
const offsetAfter = await page.$eval('#thread', (t, n) => {
  const a = t.querySelector(`article[data-n="${n}"]`);
  return a ? Math.round(a.getBoundingClientRect().top - t.getBoundingClientRect().top) : null;
}, before.n);

console.log(JSON.stringify({ heights, reading: before.n, moved: offsetAfter === null ? null : offsetAfter - before.offset }));
await browser.close();
