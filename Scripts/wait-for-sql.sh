#!/bin/sh

set -e

host="$1"
shift
port="$1"
shift

echo "⏳ Waiting for $host:$port to be available..."
until nc -z "$host" "$port"; do
  sleep 2
done

echo "✅ $host:$port is available!"
exec "$@"