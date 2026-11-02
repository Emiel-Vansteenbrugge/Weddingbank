#!/usr/bin/env bash
# Production database dump. This does not restore or delete a database.
set -euo pipefail
umask 077

ENVIRONMENT="${1:-}"
KIND="${2:-}"

if [[ "$ENVIRONMENT" != "production" ]]; then
  echo "Backups are only created for production." >&2
  exit 1
fi

case "$KIND" in
  pre-migrate | daily) ;;
  *)
    echo "Backup kind must be pre-migrate or daily." >&2
    exit 1
    ;;
esac

ENV_FILE=/opt/weddingbank/prod/.env
STATE_DIR=/var/lib/weddingbank/prod
[[ -f "$ENV_FILE" ]] || {
  echo "Missing ${ENV_FILE}" >&2
  exit 1
}

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

[[ "${COMPOSE_PROJECT_NAME:-}" == "weddingbank-prod" ]] || {
  echo "Backup refused because the compose project is not weddingbank-prod." >&2
  exit 1
}
[[ -n "${POSTGRES_USER:-}" && -n "${POSTGRES_DB:-}" && -n "${POSTGRES_PASSWORD:-}" ]] || {
  echo "Production database settings are incomplete." >&2
  exit 1
}
case "$POSTGRES_PASSWORD" in
  replace-on-server | ci-not-a-secret | weddingbank_dev)
    echo "Backup refused because the database password is still a placeholder." >&2
    exit 1
    ;;
esac

container="${COMPOSE_PROJECT_NAME}-postgres"
docker inspect "$container" >/dev/null

destination_dir="/var/backups/weddingbank/prod/${KIND}"
install -d -m 700 "$destination_dir"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
sha="unknown"
if [[ -f "${STATE_DIR}/state.env" ]]; then
  sha="$(awk -F= '$1 == "CURRENT_SHA" { print $2 }' "${STATE_DIR}/state.env")"
  [[ -n "$sha" ]] || sha="unknown"
fi
dump="${destination_dir}/weddingbank-${stamp}-${sha}.dump"

docker exec "$container" pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc >"$dump"
[[ -s "$dump" ]] || {
  rm -f "$dump"
  echo "Backup dump is empty." >&2
  exit 1
}
docker exec -i "$container" pg_restore --list <"$dump" >/dev/null

if [[ -f "${STATE_DIR}/release.env" ]]; then
  cp "${STATE_DIR}/release.env" "${dump}.release.env"
fi
chmod 600 "$dump"
echo "Wrote ${dump}"
