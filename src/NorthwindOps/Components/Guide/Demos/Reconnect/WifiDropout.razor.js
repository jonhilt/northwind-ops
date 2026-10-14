// Demo only: simulates this browser losing its Wi-Fi for a few seconds.
// While the cookie lives, the server turns away this browser's requests to Blazor's hub
// (see WifiDropoutMiddleware.cs), so every reconnection attempt fails until it expires.
const cookie = "northwind-dropout";
const path = new URL("_blazor", document.baseURI).pathname;
const button = document.querySelector("[data-dropout-seconds]");
const status = document.querySelector("[data-dropout-status]");

function setCookie(seconds) {
    document.cookie = `${cookie}=1; max-age=${seconds}; path=${path}; samesite=strict`;
}

function drop() {
    // The connection only exists once the pick list's circuit has started.
    const close = window.Blazor?._internal?.forceCloseConnection;
    if (!close) {
        status.textContent = "Not connected yet";
        return;
    }

    const seconds = Number(button.dataset.dropoutSeconds);
    const back = Date.now() + seconds * 1000;
    setCookie(seconds);
    button.disabled = true;

    const timer = setInterval(tick, 250);
    function tick() {
        const left = Math.ceil((back - Date.now()) / 1000);
        if (left > 0) {
            status.textContent = `Wi-Fi off: back in ${left} s`;
            return;
        }
        clearInterval(timer);
        status.textContent = "Wi-Fi on";
        button.disabled = false;
    }
    tick();

    // Close this page's connection to the server, as a Wi-Fi drop would.
    // (A test hook Blazor's own end-to-end tests use for the same thing.)
    Promise.resolve(close()).catch(() => { });
}

// A freshly loaded page starts with the Wi-Fi on.
setCookie(0);
button.addEventListener("click", drop);
