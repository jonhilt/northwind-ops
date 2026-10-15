// The .NET 10 way to pause a hidden tab: your own script, loaded by the stock take's
// .NET 10 page. It waits 10 seconds here so the demo is quick; 2 minutes is more usual.
let timer, paused = false;
document.addEventListener("visibilitychange", async () => {
  clearTimeout(timer);
  if (document.hidden) {
    // xray:circuit-auto-pause
    // .NET 10: pause once the tab has been hidden a while, whatever is going on,
    // even a click the server is still handling.
    timer = setTimeout(async () =>
      paused = await Blazor.pauseCircuit(), 10_000);
    // /xray:circuit-auto-pause
  } else if (paused) {
    paused = !await Blazor.resumeCircuit();
  }
});
