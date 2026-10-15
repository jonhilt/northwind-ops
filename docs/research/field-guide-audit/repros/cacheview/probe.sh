#!/usr/bin/env bash
# Hits each page three times (and once from a "second visitor") and prints time, timestamps and counters.
# Start the app first: dotnet run --urls http://localhost:5143   (add -p:TryNet11Api=true to include /cacheview)
B=${B:-http://localhost:5143}
hit() {
  curl -s -o /tmp/cv.html -D /tmp/cv.h -w '%{time_total}' "$@" | xargs printf '  %5.2fs  '
  tr -d '\n' </tmp/cv.html | grep -oE '(Page rendered at|Figures calculated at) <b>[^<]*|queries run: [0-9]+ · summary components created: [0-9]+' | sed 's/<b>//' | paste -sd'|' -
  grep -iE '^(age|set-cookie|cache-control):' /tmp/cv.h | tr -d '\r' | sed 's/^/           /'
}
for p in ${PAGES:-plain memcache wholepage wholepage-form cacheview}; do
  echo "== /$p"
  for i in 1 2 3; do hit "$B/$p"; sleep 1; done
  echo "  (second visitor, with a cookie)"; hit -H 'Cookie: visitor=2' "$B/$p"
done
