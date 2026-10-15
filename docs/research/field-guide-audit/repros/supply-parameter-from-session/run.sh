#!/bin/sh
# Starts the .NET 10 repro on :5129 and drives both workarounds with curl.
cd "$(dirname "$0")"
dotnet build -v q >/dev/null || exit 1
dotnet run --no-build --urls http://localhost:5129 >/tmp/spfs.log 2>&1 &
PID=$!
until curl -s -o /dev/null localhost:5129/hidden; do sleep 0.5; done
B=http://localhost:5129; J=$(mktemp)
tok() { curl -s -c $J -b $J "$1" | grep -o 'name="__RequestVerificationToken" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"//'; }
post() { url=$1; shift; T=$(tok $url); curl -s -i -c $J -b $J -X POST $url --data-urlencode "__RequestVerificationToken=$T" "$@"; }
show() { sed 's/></>\n</g' | grep -E "$1"; }

echo "== 1a hidden fields: step 1 posts Packs[chai]=7"
post $B/hidden --data-urlencode _handler=lines --data-urlencode "Packs[chai]=7" | show '^HTTP|id="review"|name="Draft'
echo "== 1b hidden fields: refresh / leave and come back (GET /hidden)"
curl -s -c $J -b $J $B/hidden | show '<h1|id="review"|name="Packs'
echo "== 1c hidden fields: edit the hidden input to 9999 and Send"
post $B/hidden --data-urlencode _handler=send --data-urlencode "Draft[chai]=9999" | show 'id="sent"'

: > $J
echo "== 2a manual session, handler calls Save(): post Chai=7"
post $B/manual --data-urlencode _handler=lines --data-urlencode Chai=7 | grep -iE '^HTTP|^location|^set-cookie' | cut -c1-60
curl -s -c $J -b $J $B/manual-review | show 'id="review"'
: > $J
echo "== 2b manual session, handler forgets Save(): post Chai=7"
post $B/manual-forgot --data-urlencode _handler=lines --data-urlencode Chai=7 | grep -iE '^HTTP|^location|^set-cookie' | cut -c1-60
curl -s -c $J -b $J $B/manual-review | show 'id="review"'
echo "== warnings/errors in server log:"; grep -ciE 'warn|fail|exception' /tmp/spfs.log
kill $PID; rm -f $J
