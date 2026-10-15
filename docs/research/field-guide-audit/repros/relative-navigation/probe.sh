#!/usr/bin/env bash
# Drives the .NET 10 repro with curl. Start the app first: dotnet run --urls http://localhost:5277
B=${1:-http://localhost:5277}
echo "== GET /orders/11077 (page redirects to a tab with NavigateTo(\"summary\"))"
curl -s -o /dev/null -D - "$B/orders/11077" | grep -iE '^(HTTP|location)'
echo
echo "== GET /orders/11077/summary : rendered tab hrefs"
curl -s "$B/orders/11077/summary" | grep -oE '<nav id="[a-z]+"|<a [^>]*>' 
echo
echo "== GET /lines (where the bare href=\"lines\" tab goes)"
curl -s -o /dev/null -w '%{http_code}\n' "$B/lines"
