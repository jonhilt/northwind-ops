#!/usr/bin/env bash
# Drives the running repro (dotnet run --urls http://localhost:5437) with curl.
B=${B:-http://localhost:5437}; J=$(mktemp)
tok() { curl -s -c $J -b $J "$B/orders/11078" | grep -o 'name="__RequestVerificationToken" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"$//'; }
banner() { grep -o '<div id="[a-z-]*-flash">[^<]*</div>' || echo "(no banner)"; }

echo "== Workaround 1: flag in the URL =="
T=$(tok)
curl -s -o /dev/null -D - -c $J -b $J -X POST "$B/orders/11078" -d "_handler=save-query&__RequestVerificationToken=$T" | grep -iE '^(HTTP|location)'
echo "-- GET /orders?saved=11078 (after redirect):";  curl -s -b $J "$B/orders?saved=11078" | banner
echo "-- refresh, same URL:";                          curl -s -b $J "$B/orders?saved=11078" | banner
echo "-- someone opens a shared/bookmarked link (fresh browser, no cookies, nothing saved):"; curl -s "$B/orders?saved=99999" | banner

echo; echo "== Workaround 2: hand-rolled cookie =="
T=$(tok)
curl -s -o /dev/null -D - -c $J -b $J -X POST "$B/orders/11078" -d "_handler=save-cookie&__RequestVerificationToken=$T" | grep -iE '^(HTTP|location|set-cookie)'
echo "-- GET /orders:";            curl -s -D /tmp/td-h -c $J -b $J "$B/orders" | banner; grep -i set-cookie /tmp/td-h
echo "-- refresh:";                curl -s -c $J -b $J "$B/orders" | banner
echo "-- forged cookie, no save at all:"; curl -s -H 'Cookie: Status=Your%20account%20is%20suspended%2C%20call%20555-0100' "$B/orders" | banner
echo "-- slow redirect: save, wait 6s (past the 5s MaxAge), then load the list:"
T=$(tok); curl -s -o /dev/null -c $J -b $J -X POST "$B/orders/11078" -d "_handler=save-cookie&__RequestVerificationToken=$T"
sleep 6; curl -s -c $J -b $J "$B/orders" | banner
rm -f $J
