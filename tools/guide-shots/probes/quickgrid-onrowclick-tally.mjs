// Clicks the same cells in both of the row-click entry's grids and reads each tally.
import { chromium } from "playwright";
const [base = "http://localhost:5321", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/quickgrid-onrowclick/try`, { waitUntil: "networkidle" });
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.locator("[data-tally]").waitFor();
  await page.waitForTimeout(1500); // the circuit starts
  for (const [row, col] of [[1, 2], [2, 3], [3, 1]]) {
    await frame.locator(`tbody tr:nth-child(${row}) td:nth-child(${col})`).click();
    await page.waitForTimeout(400);
  }
  console.log(side, await frame.locator("[data-tally]").innerText());
}
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
