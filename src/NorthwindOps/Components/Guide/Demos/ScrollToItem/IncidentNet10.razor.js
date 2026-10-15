// The .NET 10 workaround's JavaScript: scroll the list's box to a pixel offset, once the list
// has sized its spacers (a frame or two after it first renders).
window.scrollToOffset = (box, top) => {
    if (box.scrollHeight > box.clientHeight) box.scrollTop = top;
    else requestAnimationFrame(() => window.scrollToOffset(box, top));
};
