// Probe: .NET 10 QuickGrid has no row click (repros/quickgrid-onrowclick).
// Usage: node probes/quickgrid-onrowclick.mjs [baseUrl]
import { chromium } from 'playwright';
const base = process.argv[2] ?? 'http://localhost:5391';
const browser = await chromium.launch();
const page = await browser.newPage();
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
const ready = async () => { await page.waitForFunction(() => window.Blazor !== undefined); await page.waitForTimeout(1500); };
const state = async () => `${await page.textContent('#open')}${(await page.$('#clicks')) ? ' | ' + await page.textContent('#clicks') : ''}`;
const cursor = sel => page.$eval(sel, e => getComputedStyle(e).cursor);

console.log('--- A. "/" button in every row');
await page.goto(base); await ready();
console.log('start:', await state());
await page.click('tbody tr:nth-child(2) td:nth-child(2)'); await page.waitForTimeout(400);
console.log('click Subject cell of row 2:', await state(), '| cursor on cell:', await cursor('tbody tr:nth-child(2) td:nth-child(2)'));
await page.click('tbody tr:nth-child(2) td:nth-child(3)'); await page.waitForTimeout(400);
console.log('click Customer cell of row 2:', await state());
await page.click('tbody tr:nth-child(2) button'); await page.waitForTimeout(400);
console.log('click the Open button in row 2:', await state(), '| cursor on button:', await cursor('tbody tr:nth-child(2) button'));

console.log('\n--- B. "/try-onrowclick" OnRowClick="Open" on .NET 10 QuickGrid (it compiled with no warning)');
await page.goto(base + '/try-onrowclick'); await ready();
console.log('table attributes:', await page.$eval('table', t => [...t.attributes].map(a => `${a.name}="${a.value}"`).join(' ')));
console.log('first row attributes:', await page.$eval('tbody tr', t => [...t.attributes].map(a => `${a.name}="${a.value}"`).join(' ')));
await page.click('tbody tr:nth-child(1) td:nth-child(2)'); await page.waitForTimeout(400);
console.log('click row 1:', await state(), '| cursor on row:', await cursor('tbody tr:nth-child(1) td'));

console.log('\n--- C. "/div-in-cells" a clickable <div> in every TemplateColumn cell');
await page.goto(base + '/div-in-cells'); await ready();
const td = await page.$('tbody tr:nth-child(3) td:nth-child(2)');
const b = await td.boundingBox();
const div = await (await td.$('div')).boundingBox();
console.log(`row 3 Subject cell box ${Math.round(b.width)}x${Math.round(b.height)}, the clickable div inside it ${Math.round(div.width)}x${Math.round(div.height)}`);
await page.mouse.click(b.x + 4, b.y + 3); await page.waitForTimeout(400);
console.log('click cell padding (top-left corner of row 3 Subject cell):', await state());
await page.mouse.click(div.x + 5, div.y + div.height / 2); await page.waitForTimeout(400);
console.log('click the text in that cell:', await state(), '| row 3 class:', await page.$eval('tbody tr:nth-child(3)', r => r.className));
console.log('\nconsole errors:', errors.length ? errors : 'none');
await browser.close();
