// Drives the .NET 10 circuit-auto-pause repro (docs/research/field-guide-audit/repros/circuit-auto-pause).
// Usage: node probes/circuit-auto-pause.mjs [base]
// Headless Chromium never hides a tab, so "hide"/"show" override document.visibilityState and fire visibilitychange,
// which is exactly what a real tab switch does as far as the page's scripts are concerned.
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5128';
const browser = await chromium.launch();
const sleep = ms => new Promise(r => setTimeout(r, ms));
const circuits = async () => JSON.stringify(await (await fetch(base + '/circuits')).json());
const setVis = (page, hidden) => page.evaluate(h => {
  Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => h ? 'hidden' : 'visible' });
  Object.defineProperty(document, 'hidden', { configurable: true, get: () => h });
  document.dispatchEvent(new Event('visibilitychange'));
}, hidden);
const state = async page => ({
  circuit: await page.textContent('#circuit'),
  counted: await page.textContent('#counted'),
  noteBox: await page.inputValue('#note'),
  serverNote: await page.textContent('#saved'),
  plain: await page.textContent('#plain'),
});

async function scenario(title, path, { commitNote, typeAfter, hiddenMs }) {
  console.log(`\n== ${title} (${path})`);
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  page.on('console', m => { if (/pause|resum|circuit/i.test(m.text())) console.log('   [browser]', m.text()); });
  await page.goto(base + path);
  await page.waitForSelector('#circuit:not(:empty)');
  await page.click('#plus'); await page.click('#plus');
  await page.fill('#note', commitNote); await page.press('#note', 'Tab');   // committed: onchange reaches the server
  if (typeAfter) { await page.click('#note'); await page.fill('#note', typeAfter); } // typed, cursor still in the box
  await sleep(300);
  console.log('   before hide :', JSON.stringify(await state(page)), '| server', await circuits());
  await setVis(page, true);
  for (let t = 0; t < hiddenMs; t += 2000) { await sleep(2000); console.log(`   hidden ${String((t + 2000) / 1000).padStart(2)} s:`, await circuits()); }
  await setVis(page, false);
  await sleep(2500);
  console.log('   after show  :', JSON.stringify(await state(page)), '| server', await circuits());
  await ctx.close();
  await sleep(500);
}

await scenario('A. .NET 10, no script: hidden tab keeps its circuit', '/', { commitNote: 'Shelf 4 half empty', hiddenMs: 20000 });
await scenario('B. .NET 10 DIY script, nothing being edited', '/?diy=1', { commitNote: 'Shelf 4 half empty', hiddenMs: 6000 });
await scenario('C. .NET 10 DIY script, cursor left in a half-typed note', '/?diy=1', { commitNote: 'Shelf 4 half empty', typeAfter: 'Shelf 4 half empty, and the chai pallet is', hiddenMs: 6000 });
await browser.close();
