// Clicks each label on the .NET 10 repro and reports which element got focus,
// plus each input's accessible name. Usage: node probes/label-and-input-ids.mjs [url]
import { chromium } from 'playwright';
const url = process.argv[2] ?? 'http://localhost:5334/';
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(url);
for (const [label, input] of [['#a-label', 'Input.Company'], ['#b-label', 'Input.Email'], ['#c-label', 'Input.Phone']]) {
  await page.evaluate(() => document.activeElement?.blur());
  await page.click(label, { position: { x: 5, y: 5 } });
  const focused = await page.evaluate(() => document.activeElement?.getAttribute('name') ?? document.activeElement?.tagName);
  const snap = await page.locator(`input[name="${input}"]`).ariaSnapshot();
  console.log(`${label} "${(await page.textContent(label)).trim()}" -> focus: ${focused}; ${input} accessible: ${snap}`);
}
await browser.close();
