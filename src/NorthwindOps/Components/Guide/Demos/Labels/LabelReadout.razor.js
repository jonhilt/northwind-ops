// Reads the form as the browser sees it: each input's id, the label the browser ties to it
// (input.labels), labels whose `for` matches no id, the message under each field, and what has focus.
(() => {
    // Looked up each time: an enhanced post may hand the page a fresh, empty panel.
    const panel = () => document.querySelector("[data-label-readout]");
    if (!panel()) return;
    let drawn = null, last = "";
    const esc = s => String(s).replace(/[&<>"]/g, c => `&#${c.charCodeAt(0)};`);
    const code = s => `<code>${esc(s)}</code>`;
    // A label's own words, without the input it may wrap.
    const words = label => [...label.childNodes].filter(n => n.nodeType === 3)
        .map(n => n.textContent).join(" ").replace(/\s+/g, " ").trim();
    const tag = input => `<input${input.id ? ` id="${input.id}"` : ""} name="${input.name}">`;
    // On the .NET 10 side: an id made from the field's name is .NET 11's, not the page's.
    const added = input => panel()?.dataset.net10 && input.id === input.name.replaceAll(".", "_");

    const draw = () => {
        const form = document.querySelector("form");
        if (!form) return;
        const rows = [];
        for (const input of form.querySelectorAll("input:not([type=hidden])")) {
            const label = input.labels[0];
            const anchor = input.closest("label") ?? input;
            const next = anchor.nextElementSibling;
            const message = next?.matches(".validation-message") ? next.textContent.trim() : null;
            const by = !label ? null
                : label.contains(input) ? "the label around it" : code(`<label for="${label.htmlFor}">`);
            const named = label ? `named "${esc(words(label))}" by ${by}` : "no label names it";
            const drift = label && message && !message.includes(words(label));
            const note = added(input)
                ? `<br><i>id="${esc(input.id)}" is .NET 11's: .NET 10 rendered this input with no id</i>` : "";
            rows.push(`<li class="${label && !drift ? "ok" : "bad"}">${code(tag(input))} ${named}${note}` +
                (message ? `<br>message: "${esc(message)}"${drift ? " — not the label's name" : ""}` : "") + "</li>");
        }
        for (const label of form.querySelectorAll("label[for]")) {
            if (!label.control) {
                rows.push(`<li class="bad">${code(`<label for="${label.htmlFor}">`)} ${esc(words(label))}: ` +
                    `no element has id="${esc(label.htmlFor)}"</li>`);
            }
        }
        const focused = document.activeElement;
        const focus = focused?.matches("input") ? code(focused.id || focused.name) : "nothing";
        const net10 = panel()?.dataset.net10 ? " (.NET 10's markup, running live on .NET 11)" : "";
        const html = `<b>What the browser sees</b>${net10}<ul>${rows.join("")}</ul>` +
            `<p data-focus>Focus: ${focus}</p>`;
        const out = panel();
        if (out && (out !== drawn || html !== last || !out.firstChild)) {
            out.innerHTML = last = html;
            drawn = out;
        }
    };

    // Redraw when the form re-renders (an enhanced post) and when focus moves.
    new MutationObserver(draw).observe(document.body, { childList: true, subtree: true, attributes: true });
    document.addEventListener("focusin", draw);
    document.addEventListener("focusout", () => setTimeout(draw));
    document.addEventListener("click", () => setTimeout(draw));
    draw();
})();
