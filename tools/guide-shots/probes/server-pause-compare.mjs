// Pauses a tab from the server both ways, resumes it, and reports what the server heard back.
import { chromium } from "playwright";
const base = process.argv[2] ?? "http://localhost:5321";
const browser = await chromium.launch();
for (const [side, path] of [["net10", "/demo/server-pause/new-order/net10"], ["net11", "/demo/server-pause/new-order"]]) {
  const page = await browser.newPage();
  await page.goto(base + path, { waitUntil: "networkidle" });
  await page.waitForSelector("#quantity");
  await page.fill("#quantity", "42");
  await page.locator("#quantity").blur();
  await page.getByRole("button", { name: "Pause this tab from the server" }).click();
  await page.getByRole("button", { name: /resume/i }).click({ timeout: 15000 });
  await page.waitForSelector("[data-request]", { timeout: 15000 });
  await page.waitForTimeout(500);
  console.log(side, "|", await page.locator("[data-request]").innerText(), "| quantity", await page.inputValue("#quantity"));
  await page.close();
}
await browser.close();
