#!/usr/bin/env bash

echo "URL to download cssharp update:"

read cssUrl

echo "preparing..."

rm -rf /tmp/cssharp
mkdir /tmp/cssharp

echo "downloading cssharp from $cssUrl"

cd /tmp/cssharp && curl -LO $cssUrl

modsdir=/home/trog/serverfiles/game/csgo

echo "download complete. extracting to $modsdir"

unzip -o ./*.zip -d $modsdir 

cd /home/trog

echo "cleaning up..."

rm -rf /tmp/cssharp
