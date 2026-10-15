// The validation-localization entry after pressing save on both forms: what a learner sees after step 2.
import { chromium } from "playwright";
const [out, base = "http://localhost:5321"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/validation-localization/try`, { waitUntil: "networkidle" });
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.getByRole("button", { name: "Lieferant speichern" }).click();
  await frame.locator(".validation-message").first().waitFor();
}
await page.waitForTimeout(1000);
await page.screenshot({ path: out, fullPage: true });
await browser.close();
