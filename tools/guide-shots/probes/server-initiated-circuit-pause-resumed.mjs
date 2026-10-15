// The server-pause entry after pausing and resuming both tabs: what a learner sees after step 2.
import { chromium } from "playwright";
const [out, base = "http://localhost:5321"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/server-initiated-circuit-pause/try`, { waitUntil: "networkidle" });
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.locator("#quantity").fill("42");
  await frame.locator("#quantity").blur();
  await frame.getByRole("button", { name: "Pause this tab from the server" }).click();
  await page.waitForTimeout(3000); // paused for a moment, as a person would be
  await frame.getByRole("button", { name: /resume/i }).click({ timeout: 15000 });
  await frame.locator("[data-request]").waitFor({ timeout: 15000 });
}
await page.waitForTimeout(1000);
await page.screenshot({ path: out, fullPage: true });
await browser.close();
