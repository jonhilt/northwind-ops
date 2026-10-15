// Reads both DisplayName demos' form labels and grid headers in English, clicks Deutsch on each,
// and reads them again. With a path argument, saves the guide's try page after the switch.
import { chromium } from "playwright";
const [base = "http://localhost:5404", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/displayname-component/try`, { waitUntil: "networkidle" });

async function read(side) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.locator("[data-form-labels]").waitFor();
  const labels = await frame.locator("[data-form-labels] b").allInnerTexts();
  const headers = (await frame.locator("thead th").allInnerTexts()).map(t => t.trim());
  const matching = headers.filter((h, i) => h.toLowerCase() === labels[i].toLowerCase()).length;
  return `labels [${labels.join(", ")}] · headers [${headers.join(", ")}] · ${matching} of ${labels.length} match`;
}

for (const side of ["before", "after"]) console.log(side, "English:", await read(side));
for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.getByRole("link", { name: "Deutsch" }).click();
  await frame.locator("[data-form-labels] b", { hasText: "Lieferantencode" }).waitFor();
  console.log(side, "Deutsch:", await read(side));
}
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
