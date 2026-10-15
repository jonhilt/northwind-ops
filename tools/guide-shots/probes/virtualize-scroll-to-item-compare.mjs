// Probe: the .NET 10 offset maths beside .NET 11's InitialItemIndex / ScrollToItemAsync, both live.
// Drives each demo the way "See it work" says and prints the readout under each list.
// Usage: node probes/virtualize-scroll-to-item-compare.mjs [baseUrl] [screenshot path of the guide's try page]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5416';
const shot = process.argv[3];
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 700, height: 900 } });
// The readout, plus where the box is scrolled to and where the asked-for message really sits in it.
const measure = f => f.evaluate(() => {
  const box = document.querySelector('.thread');
  const a = [...box.querySelectorAll('article')].find(x => x.classList.contains('target'));
  const at = a ? Math.round(box.scrollTop + a.getBoundingClientRect().top - box.getBoundingClientRect().top) : null;
  return `${document.querySelector('[data-thread-readout]').textContent}\n                [scrollTop ${Math.round(box.scrollTop)}px; asked-for message sits at ${at}px]`;
});
const readout = () => measure(page);
const settle = () => page.waitForTimeout(1500);

for (const [label, path] of [['.NET 10', 'demo/scroll-to-item/incident/net10'], ['.NET 11', 'demo/scroll-to-item/incident']]) {
  console.log(`\n=== ${label}: /${path}?message=300`);
  await page.goto(`${base}/${path}?message=300`, { waitUntil: 'commit' });
  await page.waitForSelector('[data-thread-readout]');
  await page.waitForTimeout(150);
  console.log('first paint   :', await readout());
  await page.waitForSelector('form.tools button:not([disabled])');
  await settle();
  console.log('opened        :', await readout());
  await page.fill('#jump', '35');
  await page.click('form.tools button:not([type=button])');
  await settle();
  console.log('Go to 35      :', await readout());
  await page.click('form.tools button[type=button]');
  await settle();
  console.log('Latest        :', await readout());
}

if (shot) {
  // The guide's try page after the same interaction: open at 300, then Go to 35 on both sides.
  const guide = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  await guide.goto(`${base}/guide/virtualize-scroll-to-item/try`);
  await guide.waitForTimeout(2500);
  for (const frame of guide.frames().filter(f => f.url().includes('/demo/scroll-to-item'))) {
    await frame.waitForSelector('form.tools button:not([disabled])');
    await frame.waitForTimeout(1200);
    console.log('guide frame', frame.url().replace(base, ''), 'opened →', await measure(frame));
    await frame.fill('#jump', '35');
    await frame.click('form.tools button:not([type=button])');
  }
  await guide.waitForTimeout(1500);
  for (const frame of guide.frames().filter(f => f.url().includes('/demo/scroll-to-item')))
    console.log('guide frame', frame.url().replace(base, ''), '→', await measure(frame));
  await guide.screenshot({ path: shot });
}
await browser.close();
