// Runs both of the async validation entry's failure cases on the .NET 10 and .NET 11 forms.
import { chromium } from "playwright";
const base = process.argv[2] ?? "http://localhost:5321";
const browser = await chromium.launch();
const open = async path => {
  const page = await browser.newPage();
  await page.goto(base + path, { waitUntil: "networkidle" });
  await page.waitForTimeout(1500); // the circuit starts
  return page;
};
const code = page => page.getByLabel("Supplier code");
const message = page => page.$$eval(".validation-message", m => m.map(e => e.textContent).join(" / ") || "(none)");
for (const [side, path] of [["net10", "/demo/async-validation/suppliers/new/net10"], ["net11", "/demo/async-validation/suppliers/new"]]) {
  // A: a taken code, then Save straight away.
  let page = await open(path);
  await page.getByLabel("Company").fill(`Probe ${side}`);
  await code(page).fill("EXOT-01");
  await code(page).press("Tab");
  await page.getByRole("button", { name: /Save supplier|Checking/ }).click();
  await page.waitForTimeout(2500);
  const saved = await page.$eval("[data-saved], [data-rejected]", e => e.textContent).catch(() => "(nothing saved)");
  console.log(side, "A |", saved.trim(), "|", await message(page));
  await page.close();
  // B: a taken code, then a free one before the first check ends.
  page = await open(path);
  await code(page).fill("EXOT-01");
  await code(page).press("Tab");
  await page.waitForTimeout(300);
  await code(page).fill("ZZZZ-99");
  await code(page).press("Tab");
  await page.waitForTimeout(2500);
  console.log(side, "B | field:", await code(page).inputValue(), "|", await message(page));
  await page.close();
}
await browser.close();
