// Probe: .NET 10 Virtualize can't open at / scroll to an item (repros/virtualize-scroll-to-item).
// Usage: node probes/virtualize-scroll-to-item.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5851';
const browser = await chromium.launch();
const page = await browser.newPage();
// What is at the top edge of the scroll box, and how many messages are in the DOM?
const view = () => page.evaluate(() => {
  const box = document.querySelector('.thread'); const top = box.getBoundingClientRect().top;
  const arts = [...box.querySelectorAll('article')];
  const atTop = arts.find(a => a.getBoundingClientRect().bottom > top + 1);
  const t = document.getElementById('msg-240');
  return { inDom: arts.length, range: arts.length ? `${arts[0].id}..${arts.at(-1).id}` : '-', atTop: atTop?.id ?? 'none',
    scrollTop: Math.round(box.scrollTop), msg240: t ? Math.round(t.getBoundingClientRect().top - top) + 'px from top' : 'not in DOM',
    status: document.getElementById('status')?.textContent };
});
const ready = async () => { await page.waitForFunction(() => window.Blazor !== undefined); await page.waitForTimeout(1500); };

for (const p of ['offset', 'byid', 'all']) {
  const t0 = Date.now();
  await page.goto(`${base}/${p}?message=240`, { waitUntil: 'commit' });
  await page.waitForSelector('.thread');
  console.log(`\n--- /${p}?message=240`);
  console.log('first paint (prerendered HTML):', JSON.stringify(await view()));
  await ready();
  console.log('after circuit + OnAfterRender  :', JSON.stringify(await view()), `(${Date.now() - t0}ms)`);
}

console.log('\n--- /jump: type 35, press Go (offset maths, no ScrollToItemAsync)');
await page.goto(base + '/jump'); await ready();
await page.fill('#n', '35'); await page.click('#go'); await page.waitForTimeout(800);
console.log(await page.evaluate(() => {
  const box = document.querySelector('.thread'); const top = box.getBoundingClientRect().top;
  const atTop = [...box.querySelectorAll('article')].find(a => a.getBoundingClientRect().bottom > top + 1);
  const t = document.getElementById('msg-35');
  return `at top: ${atTop?.id}; msg-35: ${t ? Math.round(t.getBoundingClientRect().top - top) + 'px from top' : 'not in DOM'}`;
}));
await page.screenshot({ path: new URL('./virtualize-scroll-to-item.png', import.meta.url).pathname });
await browser.close();
