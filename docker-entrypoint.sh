#!/bin/sh

set -e

data_init="/app/data_init"
data="/app/data"

cp -R -n $data_init/* $data
cp -n "$data_init/creds_example.yml" "$data/creds.yml"

# Always overwrite native libraries to ensure updates are applied
cp -f "$data_init/lib/"*.so "$data/lib/" 2>/dev/null || true

yt-dlp -U || true

exec "$@"