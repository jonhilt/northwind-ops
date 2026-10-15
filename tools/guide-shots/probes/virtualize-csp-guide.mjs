// Scrolls both of the CSP entry's lists to the end, and reports how far each got and its CSP errors.
import { chromium } from "playwright";
const [base = "http://localhost:5321", shot] = process.argv.slice(2);
const browser = await chromium.launch();
for (const [side, path, box, row] of [
  ["net10", "/demo/virtualize-csp/orders/net10", "[data-net10-list]", ".row"],
  ["net11", "/demo/virtualize-csp/orders", ".orders", ".order b"]]) {
  const page = await browser.newPage();
  const csp = [];
  page.on("console", m => { if (m.text().includes("Content Security Policy")) csp.push(m.text()); });
  await page.goto(base + path, { waitUntil: "networkidle" });
  await page.waitForTimeout(1500);
  for (let i = 0; i < 40; i++) {
    await page.$eval(box, b => b.scrollTop = b.scrollHeight);
    await page.waitForTimeout(100);
  }
  await page.waitForTimeout(800);
  const last = await page.$$eval(row, rs => rs.at(-1)?.textContent?.trim());
  const height = await page.$eval(box, b => b.scrollHeight);
  console.log(side, "| last row", last, "| scrollHeight", height, "| CSP errors", csp.filter(t => t.includes("inline style")).length);
  await page.close();
}
if (shot) {
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  await page.goto(`${base}/guide/virtualize-csp/try`, { waitUntil: "networkidle" });
  await page.waitForTimeout(2000);
  for (const [side, box] of [["before", "[data-net10-list]"], ["after", ".orders"]]) {
    const frame = page.frameLocator(`[data-side=${side}] iframe`);
    for (let i = 0; i < 40; i++) { await frame.locator(box).evaluate(b => b.scrollTop = b.scrollHeight); await page.waitForTimeout(80); }
  }
  await page.waitForTimeout(1000);
  await page.screenshot({ path: shot, fullPage: true });
}
await browser.close();
