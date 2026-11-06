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

if [[ -n "${WEDDINGBANK_ENV_FILE:-}" ]]; then
  ENV_FILE="$WEDDINGBANK_ENV_FILE"
elif [[ -f /home/administrator/config/weddingbank/.env ]]; then
  ENV_FILE=/home/administrator/config/weddingbank/.env
elif [[ -f /opt/weddingbank/prod/.env ]]; then
  ENV_FILE=/opt/weddingbank/prod/.env
else
  echo "Missing production .env. Expected /home/administrator/config/weddingbank/.env" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a
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

container="${WEDDINGBANK_POSTGRES_CONTAINER:-weddingbank-postgres}"
docker inspect "$container" >/dev/null

destination_dir="/var/backups/weddingbank/prod/${KIND}"
install -d -m 700 "$destination_dir"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
dump="${destination_dir}/weddingbank-${stamp}.dump"

docker exec "$container" pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc >"$dump"
[[ -s "$dump" ]] || {
  rm -f "$dump"
  echo "Backup dump is empty." >&2
  exit 1
}
docker exec -i "$container" pg_restore --list <"$dump" >/dev/null

chmod 600 "$dump"
echo "Wrote ${dump}"
