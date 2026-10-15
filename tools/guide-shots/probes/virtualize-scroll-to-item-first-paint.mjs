// Probe: what each side shows before Blazor starts (the prerendered HTML, JavaScript off), and
// the moment after. Usage: node probes/virtualize-scroll-to-item-first-paint.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5416';
const browser = await chromium.launch();
const view = page => page.evaluate(() => {
  const box = document.querySelector('.thread'); const top = box.getBoundingClientRect().top;
  const shown = [...box.querySelectorAll('article')];
  const n = a => a.querySelector('header').textContent.match(/#(\d+)/)[1];
  if (!shown.length) return `scrollTop ${Math.round(box.scrollTop)} · no messages on the page, box ${box.scrollHeight}px tall`;
  const first = shown.find(a => a.getBoundingClientRect().top >= top - 1);
  return `scrollTop ${Math.round(box.scrollTop)} · top of the box #${first && n(first)} · on the page #${n(shown[0])}–#${n(shown.at(-1))}`;
});
for (const path of ['demo/scroll-to-item/incident/net10', 'demo/scroll-to-item/incident']) {
  const off = await browser.newPage({ javaScriptEnabled: false });
  await off.goto(`${base}/${path}?message=300`);
  console.log(`${path} prerendered:`, await view(off));
}
await browser.close();
