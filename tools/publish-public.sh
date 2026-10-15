#!/usr/bin/env bash
# Publishes this repo's main branch to a separate public repo, without its history.
#
# The private repo keeps everything (history, deploy files, other branches). The public one gets only the files at
# the tip of main, as one new commit each time you run this: the first run creates its first commit, later runs add
# a "Sync" commit on top. Nothing from the private history ever reaches it.
#
# Usage: tools/publish-public.sh [--push] <public repo URL> [author, default "Jon Hilton <jon@jonhilton.net>"]
#   e.g. tools/publish-public.sh git@github.com:jonhilt/northwind-ops.git
# Create the public repo on GitHub first (empty: no README or licence). Without --push the script stops before
# pushing, so you can look it over. With --push (as .github/workflows/publish-public.yml runs it) it pushes, and the
# secret scan is required rather than skipped.
set -euo pipefail

push=false
if [[ ${1:-} == --push ]]; then push=true; shift; fi
public=${1:?usage: tools/publish-public.sh [--push] <public repo URL> [author]}
author=${2:-"Jon Hilton <jon@jonhilton.net>"}

# Left out of the public copy: how and where it's hosted, which only the private repo deploys from, and the
# workflow that publishes this copy, which would fail in the public repo with no key.
exclude=(deploy .github/workflows/deploy.yml .github/workflows/publish-public.yml)

private=$(git rev-parse --show-toplevel)
git -C "$private" fetch -q origin main
sha=$(git -C "$private" rev-parse --short origin/main)

work=$(mktemp -d)
echo "Working in $work"

# The public repo as it stands, or a new one.
if git clone -q "$public" "$work/public" 2>/dev/null && git -C "$work/public" rev-parse -q --verify HEAD >/dev/null; then
  message="Sync with Northwind Ops at $sha"
else
  rm -rf "$work/public"
  git init -q -b main "$work/public"
  git -C "$work/public" remote add origin "$public"
  message="Northwind Ops: a Blazor showcase of .NET 11"
fi

# Replace its files with main's, minus the excluded paths.
find "$work/public" -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
git -C "$private" archive origin/main | tar -x -C "$work/public"
for path in "${exclude[@]}"; do rm -rf "${work:?}/public/$path"; done

# Refuse to publish anything that looks like a secret, or the server's address.
if command -v gitleaks >/dev/null; then
  gitleaks detect --source "$work/public" --no-git --no-banner --redact
elif $push; then
  echo "gitleaks isn't installed, and --push won't publish without a secret scan." >&2
  exit 1
else
  echo "gitleaks isn't installed (brew install gitleaks): skipping the secret scan" >&2
fi
if grep -rInE '([0-9]{1,3}\.){3}[0-9]{1,3}' "$work/public" --exclude-dir=.git --exclude='*.json' | grep -vE 'Version *=|127\.0\.0\.1|0\.0\.0\.0|203\.0\.113\.|198\.51\.100\.|192\.0\.2\.'; then
  echo "Found what look like IP addresses above. Check them, then run again." >&2
  exit 1
fi

git -C "$work/public" add -A
if git -C "$work/public" diff --cached --quiet; then
  echo "The public repo already matches main at $sha."
  exit 0
fi
git -C "$work/public" -c user.name="${author% <*}" -c user.email="$(sed 's/.*<\(.*\)>/\1/' <<<"$author")" \
  commit -q -m "$message" --author="$author"

echo
# --stat-count rather than "| head": with pipefail, head closing the pipe early fails the run (SIGPIPE, exit 141).
git -C "$work/public" show --stat --stat-count=20 --format='%an <%ae>%n%s' HEAD
echo
if $push; then
  git -C "$work/public" push -q -u origin main
  echo "Pushed to $public."
else
  echo "Ready. Look it over in $work/public, then push it:"
  echo "  git -C $work/public push -u origin main"
fi
