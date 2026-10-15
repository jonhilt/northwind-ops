// On the guide's try page: make the same mistakes in both forms, then fix them and save.
// Prints each side's messages and tally after every step. Usage:
// node probes/ssr-client-validation-sides.mjs [baseUrl] [after-save.png] [after-tab.png]
import { chromium } from "playwright";
const [base = "http://localhost:5432", shot, tabShot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
const posts = { before: 0, after: 0 };
page.on("request", r => {
  if (r.method() !== "POST") return;
  if (r.url().includes("/demo/client-validation/")) posts[r.url().endsWith("/net10") ? "before" : "after"]++;
});
await page.goto(`${base}/guide/ssr-client-validation/try`, { waitUntil: "networkidle" });
await page.waitForTimeout(1000);
const frame = side => page.frameLocator(`[data-side=${side}] iframe`);
async function report(step) {
  for (const side of ["before", "after"]) {
    const f = frame(side);
    const msgs = await f.locator(".validation-message:visible").allInnerTexts();
    const tally = (await f.locator("[data-tally]").innerText()).replace(/\s+/g, " ");
    console.log(`${step} | ${side} | ${tally} | POSTs seen: ${posts[side]} | ${JSON.stringify(msgs)}`);
  }
}
await report("0 loaded");
for (const side of ["before", "after"]) {
  await frame(side).locator("#email").fill("sales@");
  await frame(side).locator("#email").press("Tab");
}
await page.waitForTimeout(1000);
await report("1 sales@ + Tab");
if (tabShot) await page.screenshot({ path: tabShot, fullPage: true });
for (const side of ["before", "after"]) await frame(side).getByRole("button", { name: "Save supplier" }).click();
await page.waitForTimeout(1500);
await report("2 Save, Company empty");
if (shot) await page.screenshot({ path: shot, fullPage: true });
for (const side of ["before", "after"]) {
  await frame(side).locator("#company").fill(`Guide ${side}`);
  await frame(side).locator("#email").fill("sales@guide.test");
  await frame(side).getByRole("button", { name: "Save supplier" }).click();
}
await page.waitForTimeout(1500);
await report("3 fixed + Save");
for (const side of ["before", "after"]) console.log(side, "flash:", await frame(side).locator(".flash").innerText().catch(() => null));
await browser.close();
