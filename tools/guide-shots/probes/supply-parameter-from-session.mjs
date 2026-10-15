// Drives both sides of the session entry's try page the way its watch steps say, and prints
// what each side shows: review, refresh, change quantities, send, and a tampered hidden field.
// With a path, saves a screenshot of the try page after step 2 (the refresh); with a second,
// one after step 4 (the tampered send).
import { chromium } from "playwright";
const [base = "http://localhost:5408", shot, tamperShot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/supply-parameter-from-session/try`, { waitUntil: "networkidle" });

const side = name => ({
  frame: page.frameLocator(`[data-side=${name}] iframe`),
  bar: page.locator(`[data-side=${name}] .bar`),
});
const settle = () => page.waitForTimeout(700);
const read = async ({ frame }) => {
  const strip = await frame.locator(".kept").innerText();
  const h1 = (await frame.locator("h1").allInnerTexts())[0] ?? "(no heading)";
  const values = await frame.locator("input[type=number]").evaluateAll(els => els.map(e => e.value || "·"));
  const lines = await frame.locator("form > div").allInnerTexts();
  const flash = (await frame.locator(".flash, .warn").allInnerTexts()).join(" ");
  return `${h1} | ${strip} | boxes [${values.join(" ")}] | lines [${lines.join("; ")}] ${flash}`;
};
const L = side("before"), R = side("after");

console.log("1. Type 5 and 7, press Review order");
for (const s of [L, R]) {
  await s.frame.locator("input[type=number]").nth(0).fill("5");
  await s.frame.locator("input[type=number]").nth(1).fill("7");
  await s.frame.getByRole("button", { name: "Review order →" }).click();
  await settle();
}
console.log("  left :", await read(L));
console.log("  right:", await read(R));

console.log("2. Press the refresh button on each bar");
for (const s of [L, R]) { await s.bar.locator("[data-demo-refresh]").click(); await settle(); }
console.log("  left :", await read(L));
console.log("  right:", await read(R), "| address:", await R.bar.innerText());
if (shot) await page.screenshot({ path: shot, fullPage: true });

console.log("3. Right: Change quantities, Review, Send");
await R.frame.getByRole("link", { name: "← Change quantities" }).click(); await settle();
console.log("  right after Change:", await read(R));
await R.frame.getByRole("button", { name: "Review order →" }).click(); await settle();
await R.frame.getByRole("button", { name: "Send to supplier" }).click(); await settle();
console.log("  right after Send  :", await read(R));
await R.frame.getByRole("link", { name: "← Change quantities" }).click(); await settle();
console.log("  right, then Change:", await read(R));

console.log("4. Left: review again, set a hidden field to 9999 in DevTools, Send. Right: review again");
await L.frame.locator("input[type=number]").nth(0).fill("5");
await L.frame.getByRole("button", { name: "Review order →" }).click(); await settle();
await L.frame.locator("input[type=hidden][name^=Draft]").first().evaluate(e => (e.value = "9999"));
await L.frame.getByRole("button", { name: "Send to supplier" }).click(); await settle();
console.log("  left :", await read(L));
await R.frame.locator("input[type=number]").nth(0).fill("5");
await R.frame.getByRole("button", { name: "Review order →" }).click(); await settle();
console.log("  right:", await read(R), "| hidden inputs:", await R.frame.locator("input[type=hidden][name^=Draft]").count());
await page.getByText("Check your guess").click();
if (tamperShot) await page.screenshot({ path: tamperShot, fullPage: true });
await browser.close();
