// Probe for the web-worker-template audit repro (.NET 10).
// Run the repro first: (cd docs/research/field-guide-audit/repros/web-worker-template && dotnet run -c Release)
// Then: node probes/web-worker-template.mjs   (from tools/guide-shots)
import { chromium } from 'playwright';

const url = process.env.URL ?? 'http://localhost:5134/';
const browser = await chromium.launch();
const page = await browser.newPage();
page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.log('console.' + m.type() + ':', m.text()); });
page.on('pageerror', e => console.log('pageerror:', e.message));
await page.goto(url);
await page.waitForFunction(() => document.querySelector('#status')?.textContent.includes('Ready'), null, { timeout: 60000 });
console.log('status:', await page.textContent('#status'));

// Record every animation frame in the page. rAF only runs when the UI thread is free,
// so the biggest gap between frames is how long the page was frozen.
async function run(button, shot) {
  await page.evaluate(() => {
    window.__frames = [];
    const loop = t => { window.__frames.push(t); window.__raf = requestAnimationFrame(loop); };
    window.__raf = requestAnimationFrame(loop);
  });
  await page.waitForTimeout(300);
  const clockBefore = await page.textContent('#clock');
  await page.click(button);
  // Try to read the clock while it works (this call itself waits for the thread to free up).
  await page.waitForTimeout(800);
  const t0 = Date.now();
  const clockMid = await page.textContent('#clock');
  const evalWait = Date.now() - t0;
  await page.waitForFunction(() => document.querySelector('#status').textContent.startsWith('Totalled'), null, { timeout: 120000 });
  if (shot) await page.screenshot({ path: shot });
  const { gap, frames } = await page.evaluate(() => {
    cancelAnimationFrame(window.__raf);
    const f = window.__frames; let gap = 0;
    for (let i = 1; i < f.length; i++) gap = Math.max(gap, f[i] - f[i - 1]);
    return { gap: Math.round(gap), frames: f.length };
  });
  console.log(`\n[${button}] clock before=${clockBefore} clock read 0.8s in=${clockMid} (that read waited ${evalWait} ms)`);
  console.log(`  status: ${await page.textContent('#status')}`);
  console.log(`  animation frames recorded: ${frames}, longest gap between frames: ${gap} ms`);
}

await run('#ui', process.env.SHOT ? `${process.env.SHOT}-ui.png` : undefined);
await run('#yield', process.env.SHOT ? `${process.env.SHOT}-yield.png` : undefined);
await browser.close();
