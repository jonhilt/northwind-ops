// Virtualize's AnchorMode.End follows new rows while the list is scrolled to the bottom, but it doesn't
// tell the component whether it's following. The "Following / Paused" indicator watches the scroll
// position with the same rule Virtualize uses: within 2px of the bottom counts as the bottom.

const isAtBottom = el =>
    el.scrollHeight <= el.clientHeight || Math.abs(el.scrollTop + el.clientHeight - el.scrollHeight) < 2;

export function watchFollowing(scroller, dotnet) {
    let following = true;
    let pending = 0;

    const report = atBottom => {
        if (atBottom !== following) {
            following = atBottom;
            dotnet.invokeMethodAsync('SetFollowing', atBottom);
        }
    };

    // Reaching the bottom resumes following straight away. Leaving it is only reported once the list
    // settles, so the moment between a new row landing and Virtualize scrolling to it doesn't flicker.
    const onScroll = () => {
        clearTimeout(pending);
        if (isAtBottom(scroller)) {
            report(true);
        } else {
            pending = setTimeout(() => report(isAtBottom(scroller)), 150);
        }
    };

    scroller.addEventListener('scroll', onScroll, { passive: true });
    return {
        dispose: () => {
            clearTimeout(pending);
            scroller.removeEventListener('scroll', onScroll);
        },
    };
}

// Scrolls to the newest row. Rows below the viewport are only estimates until Virtualize renders them,
// so keep going for a few frames until the bottom stays put.
export function jumpToLatest(scroller) {
    let settled = 0;
    let frames = 0;
    const step = () => {
        scroller.scrollTop = scroller.scrollHeight;
        settled = isAtBottom(scroller) ? settled + 1 : 0;
        if (settled < 3 && ++frames < 60) {
            requestAnimationFrame(step);
        }
    };
    step();
}
