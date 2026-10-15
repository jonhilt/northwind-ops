// Saves an order on both sides of the TempData entry, refreshes both lists, then follows the
// left list's forged ?saved=99999 link. Prints what each side shows at each moment.
// With a path argument (…/2-after-interaction.png) it also saves …-saved.png after the save,
// the path itself after the refresh (with "Check your guess" open) and …-forged.png.
import { chromium } from "playwright";
const [base = "http://localhost:5444", shot] = process.argv.slice(2);
const named = (suffix) => shot && shot.replace(/\.png$/, `-${suffix}.png`);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/tempdata-ssr/try`, { waitUntil: "networkidle" });

const read = async (side) => {
  const box = page.locator(`[data-side=${side}]`);
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  await frame.locator("[data-carrier]").waitFor();
  const flash = await frame.locator(".flash").count() ? await frame.locator(".flash").innerText() : "(no message)";
  return `${await box.locator("[data-demo-address]").innerText()} | ${flash} | ${await frame.locator("[data-carrier]").innerText()}`;
};

for (const side of ["before", "after"]) {
  const frame = page.frameLocator(`[data-side=${side}] iframe`);
  console.log(side, "start  ", await read(side));
  await frame.locator("tbody tr:first-child a").click();
  await frame.locator("#notes").fill(`Probe note (${side})`);
  await frame.locator("button[type=submit]").click();
  await frame.locator(".flash").waitFor();
  await page.waitForTimeout(300);
  console.log(side, "saved  ", await read(side));
}
if (shot) await page.screenshot({ path: named("saved"), fullPage: true });

for (const side of ["before", "after"]) {
  await page.locator(`[data-side=${side}] [data-demo-refresh]`).click();
  await page.waitForTimeout(1200);
  console.log(side, "refresh", await read(side));
}
await page.getByText("Check your guess").click();
await page.waitForTimeout(300);
if (shot) await page.screenshot({ path: shot, fullPage: true });

await page.frameLocator("[data-side=before] iframe").locator("[data-forged]").click();
await page.waitForTimeout(1200);
for (const side of ["before", "after"]) console.log(side, "forged ", await read(side));
if (shot) await page.screenshot({ path: named("forged"), fullPage: true });
await browser.close();
