#!/usr/bin/env bash
set -euo pipefail

MOBILE_URL="${1:-http://127.0.0.1:8090}"
BACKEND_URL="${2:-http://127.0.0.1:5278/admin/}"

echo "Desktop URLs:"
echo "  Mobile App:   ${MOBILE_URL}"
echo "  Backend Admin:${BACKEND_URL}"

open_url() {
  local url="$1"
  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${url}" >/dev/null 2>&1 || true
    return
  fi
  if command -v open >/dev/null 2>&1; then
    open "${url}" >/dev/null 2>&1 || true
    return
  fi
  if command -v wslview >/dev/null 2>&1; then
    wslview "${url}" >/dev/null 2>&1 || true
  fi
}

open_url "${MOBILE_URL}"
open_url "${BACKEND_URL}"
