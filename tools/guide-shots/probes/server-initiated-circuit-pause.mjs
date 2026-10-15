// Drives the .NET 10 server-initiated-circuit-pause repro in a real browser, starting and
// restarting the server itself. Usage: node probes/server-initiated-circuit-pause.mjs
import { chromium } from 'playwright';
import { spawn } from 'node:child_process';

const dir = new URL('../../../docs/research/field-guide-audit/repros/server-initiated-circuit-pause/', import.meta.url).pathname;
const port = 5150, base = `http://localhost:${port}`;
let server;
const log = [];

async function start() {
  server = spawn('dotnet', ['bin/Debug/net10.0/PauseRepro.dll', '--urls', base],
    { cwd: dir, env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' } });
  server.stdout.on('data', d => String(d).split('\n').filter(l => l.includes('[repro]') || /fail|warn/.test(l))
    .forEach(l => { log.push(l.trim()); console.log('   server| ' + l.trim()); }));
  server.stderr.on('data', d => console.log('   server!| ' + String(d).trim()));
  for (let i = 0; i < 100; i++) {
    try { if ((await fetch(base)).ok) return; } catch { }
    await new Promise(r => setTimeout(r, 100));
  }
  throw new Error('server did not start');
}
async function stop(signal = 'SIGTERM') {
  const s = server; server = null;
  await new Promise(r => { s.on('exit', r); s.kill(signal); });
}
const wait = ms => new Promise(r => setTimeout(r, ms));

const browser = await chromium.launch();

async function openFilled(label) {
  console.log(`\n=== ${label}`);
  const page = await browser.newPage();
  page.on('framenavigated', f => { if (f === page.mainFrame()) console.log(`   browser| navigated/reloaded ${f.url()}`); });
  page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.log(`   console.${m.type()}| ${m.text().slice(0, 160)}`); });
  await page.goto(base);
  await page.waitForSelector('#circuit:not(:empty)');
  await page.fill('#customer', 'ALFKI'); await page.press('#customer', 'Tab');
  await page.fill('#quantity', '7'); await page.press('#quantity', 'Tab');
  await wait(300);
  await show(page, 'filled in');
  return page;
}
async function show(page, what) {
  const s = await page.evaluate(() => {
    const m = document.getElementById('components-reconnect-modal');
    const visible = m?.open ? [...m.querySelectorAll('p,button')].filter(e => getComputedStyle(e).display !== 'none').map(e => e.textContent.trim().replace(/\s+/g, ' ')) : [];
    return {
      pid: document.getElementById('pid')?.textContent, circuit: document.getElementById('circuit')?.textContent,
      customer: document.getElementById('customer')?.value, quantity: document.getElementById('quantity')?.value,
      status: document.getElementById('status')?.textContent, dialog: m?.open ? `${m.className} :: ${visible.join(' | ')}` : 'closed',
    };
  });
  console.log(`   ${what}: process ${s.pid} circuit ${s.circuit} | customer="${s.customer}" quantity="${s.quantity}" | status="${s.status}" | dialog: ${s.dialog}`);
}

await start();

// A: the failure. A deploy restarts the process under an open tab.
{
  const page = await openFilled('A. Restart the server (a deploy) with a half-filled order');
  await stop('SIGTERM'); console.log('   -- server stopped (SIGTERM)');
  await wait(1500); await show(page, 'server down');
  await start(); console.log('   -- new server process up');
  for (const t of [3000, 8000, 15000]) { await wait(t); await show(page, `+${t / 1000}s`); }
  await page.close();
}

// B: .NET 10 can pause, if the browser starts it. Pause, restart, resume.
{
  const page = await openFilled('B. Browser starts the pause (Blazor.pauseCircuit()), then restart, then Resume');
  await page.evaluate(() => Blazor.pauseCircuit()); await wait(1000);
  await show(page, 'after Blazor.pauseCircuit()');
  await stop(); await start(); console.log('   -- server restarted');
  await page.click('#components-resume-button'); await wait(2000);
  await show(page, 'after Resume');
  await page.close();
}

// C: the server asks with plain JS interop: await JS.InvokeVoidAsync("Blazor.pauseCircuit").
{
  const page = await openFilled('C. Server asks via awaited JS interop to Blazor.pauseCircuit');
  await page.click('#ask-await'); await wait(3000);
  await show(page, 'after server asked');
  if (await page.isVisible('#components-resume-button')) { await page.click('#components-resume-button'); await wait(2000); await show(page, 'after Resume'); }
  await page.close();
}

// D: the workaround from dotnet/aspnetcore#62327: a JS wrapper that returns at once.
{
  const page = await openFilled('D. Server asks via a sync JS wrapper (window.remotePause)');
  await page.click('#ask-wrapper'); await wait(3000);
  await show(page, 'after server asked');
  if (await page.isVisible('#components-resume-button')) { await page.click('#components-resume-button'); await wait(2000); await show(page, 'after Resume'); }
  await page.close();
}

await browser.close();
await stop();
