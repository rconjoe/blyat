#!/usr/bin/env bash

echo "URL to download metamod update:"

read metaUrl

echo "preparing..."

rm -rf /tmp/metamod
mkdir /tmp/metamod

echo "downloading metamod from $metaUrl"

cd /tmp/metamod && curl -LO $metaUrl

modsdir=/home/trog/serverfiles/game/csgo

echo "download complete. extracting to $modsdir"

tar -xvzf ./*.tar.gz -C $modsdir 

cd /home/trog

echo "cleaning up..."

rm -rf /tmp/metamod
