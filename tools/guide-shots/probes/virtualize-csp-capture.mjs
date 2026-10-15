// Captures the list markup a .NET 10 Virtualize renders under style-src 'self', for the guide's recording.
import { chromium } from "playwright";
import { writeFile } from "node:fs/promises";
const [base = "http://localhost:5674", out = "virtualize-csp-net10.html"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage();
const errors = [];
page.on("console", m => { if (m.type() === "error") errors.push(m.text()); });
await page.goto(base, { waitUntil: "networkidle" });
await page.waitForTimeout(2000);
const box = await page.evaluate(() => {
  const spacer = document.querySelector("[style*='height']");
  const scroller = spacer?.parentElement;
  return { html: scroller?.outerHTML, scrollHeight: scroller?.scrollHeight };
});
await writeFile(out, box.html ?? "");
console.log("scrollHeight", box.scrollHeight, "errors", errors.length, errors[0]?.slice(0, 120));
await browser.close();
