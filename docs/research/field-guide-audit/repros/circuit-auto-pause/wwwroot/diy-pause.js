// The .NET 10 workaround, exactly as the Field Guide's "The problem" shows it, with a 3 s delay instead of 120 s.
let timer, paused = false;
document.addEventListener("visibilitychange", async () => {
  clearTimeout(timer);
  if (document.hidden) {
    timer = setTimeout(async () =>
      paused = await Blazor.pauseCircuit(), 3_000);
  } else if (paused) {
    paused = !await Blazor.resumeCircuit();
  }
});
