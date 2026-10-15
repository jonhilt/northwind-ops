// Watches both inbox feeds on the guide's try page, the way its watch steps say, and prints each
// side's readout: sitting at the bottom, after scrolling up to read, and back at the bottom.
// Usage: node probes/virtualize-anchor-mode-compare.mjs [baseUrl] [screenshot.png]
import { chromium } from "playwright";
const [base = "http://localhost:5448", shot] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(`${base}/guide/virtualize-anchor-mode/try`, { waitUntil: "networkidle" });
const sides = ["before", "after"];
const feed = side => page.frameLocator(`[data-side=${side}] iframe`).locator(".feed");
const readout = side => page.frameLocator(`[data-side=${side}] iframe`).locator("[data-feed-readout]").innerText();
const print = async label => {
    for (const side of sides) console.log(label.padEnd(20), side.padEnd(7), await readout(side));
};
// Scrolls with the mouse wheel, as a reader does.
const wheel = async (side, dy, times) => {
    const box = await feed(side).boundingBox();
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    for (let i = 0; i < times; i++) {
        await page.mouse.wheel(0, dy);
        await page.waitForTimeout(40);
    }
};
// The top row in view, and where it sits, to check the rows being read stay put.
const topRow = side => feed(side).evaluate(f => {
    const box = f.getBoundingClientRect();
    const row = [...f.querySelectorAll("[data-n]")].find(r => r.getBoundingClientRect().bottom > box.top + 1);
    return `#${10900 + +row.dataset.n} at ${Math.round(row.getBoundingClientRect().top - box.top)}px`;
});

for (const side of sides) await feed(side).locator("[data-n]").first().waitFor();
await page.waitForTimeout(1500); // the circuits start
await print("on load");
// Step 1: the .NET 10 feed opens at the top, so scroll it to the bottom first.
await wheel("before", 4000, 30);
await page.waitForTimeout(500);
await print("left scrolled down");
for (let s = 1; s <= 3; s++) {
    await page.waitForTimeout(2500);
    await print(`${s * 2.5}s later`);
}

// Step 2: scroll up a little to read.
for (const side of sides) await wheel(side, -100, 4);
await page.waitForTimeout(300);
const reading = {};
for (const side of sides) reading[side] = await topRow(side);
await print("scrolled up");
await page.waitForTimeout(7500);
await print("7.5s later");
for (const side of sides) console.log(`${side}: top row ${reading[side]}, 7.5s later ${await topRow(side)}`);

// Step 3: back to the very bottom, then let more arrive.
for (const side of sides) await wheel(side, 400, 30);
await page.waitForTimeout(500);
await print("back at the bottom");
await page.waitForTimeout(7500);
await print("7.5s later");
if (shot) await page.screenshot({ path: shot, fullPage: true });
await browser.close();
