// On the guide page: type a taken code into both forms and press Save straight away, then screenshot.
import { chromium } from "playwright";
const [base = "http://localhost:5321", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/async-form-validation/try`, { waitUntil: "networkidle" });
await page.waitForTimeout(2000);
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.getByLabel("Company").fill(`Guide ${side}`);
  await frame.getByLabel("Supplier code").fill("EXOT-01");
  await frame.getByLabel("Supplier code").press("Tab");
  await frame.getByRole("button", { name: /Save supplier|Checking/ }).click();
}
await page.waitForTimeout(3000);
await page.screenshot({ path: shot, fullPage: true });
await browser.close();
