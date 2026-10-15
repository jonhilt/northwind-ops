// The nested validation entry after pressing Save on both sides: what a learner sees after step 2.
import { chromium } from "playwright";
const [out, base = "http://localhost:5321"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/validatable-type-stable/try`, { waitUntil: "networkidle" });
await page.waitForLoadState("networkidle");
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.getByRole("button", { name: "Save supplier" }).click();
}
await page.waitForTimeout(1500);
await page.screenshot({ path: out, fullPage: true });
await browser.close();
