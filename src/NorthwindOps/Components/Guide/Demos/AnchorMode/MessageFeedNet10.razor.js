// Demo only: open the feed at its newest message, as a chat would. Virtualize sizes the rows it
// hasn't drawn yet from an estimate, so scroll again once the last rows have rendered.
export function scrollToEnd(feed) {
    feed.scrollTop = feed.scrollHeight;
    setTimeout(() => feed.scrollTop = feed.scrollHeight, 200);
}
