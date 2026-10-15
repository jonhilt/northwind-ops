#!/bin/sh
# usage: ./post.sh <page> <port>   POSTs the form with Company filled and every nested field empty or invalid
page=$1; port=${2:-5788}; jar=$(mktemp)
tok=$(curl -s -c $jar http://localhost:$port/$page | grep -o 'name="__RequestVerificationToken" value="[^"]*"' | sed 's/.*value="//;s/"$//')
curl -s -b $jar http://localhost:$port/$page \
  --data-urlencode "_handler=$page" --data-urlencode "__RequestVerificationToken=$tok" \
  --data-urlencode "Input.Company=Acme" --data-urlencode "Input.Address.City=" \
  --data-urlencode "Input.Contacts[0].Name=" --data-urlencode "Input.Contacts[0].Email=sales@" \
  | grep -oE '<li class="validation-message">[^<]*</li>|<p id="result">[^<]*</p>|class="[^"]*invalid[^"]*" name="[^"]*"'
