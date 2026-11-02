#!/usr/bin/env bash
# Pull-agent for The Wedding Bank. It reads GitLab and pulls images.
# It does not publish code or images and it does not open an inbound port.
set -euo pipefail
umask 077

readonly PROJECT_PATH="evsb-group/weddingbank"
readonly API_URL="https://gitlab.com/api/v4"
readonly REGISTRY="registry.gitlab.com"
readonly IMAGE_PREFIX="registry.gitlab.com/evsb-group/weddingbank"
readonly IMAGE_API_PATTERN="^${IMAGE_PREFIX}/api@sha256:[a-f0-9]{64}$"
readonly IMAGE_WEB_PATTERN="^${IMAGE_PREFIX}/web@sha256:[a-f0-9]{64}$"
readonly IMAGE_PROXY_PATTERN="^${IMAGE_PREFIX}/proxy@sha256:[a-f0-9]{64}$"
readonly SHA_PATTERN='^[0-9a-f]{40}$'
readonly TOKEN_PATTERN='^[A-Za-z0-9._~+/-]+=*$'

log() {
  printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*" >&2
  if command -v logger >/dev/null 2>&1; then
    logger -t weddingbank-agent -- "$*" || true
  fi
}

die() {
  log "$*"
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "Missing command: $1"
}

token_usable() {
  local value="$1"
  [[ -n "$value" && "$value" != "replace-on-server" && "$value" =~ $TOKEN_PATTERN ]]
}

validate_release_manifest() {
  local file="$1"
  jq -e \
    --arg sha_pattern "$SHA_PATTERN" \
    --arg api_pattern "$IMAGE_API_PATTERN" \
    --arg web_pattern "$IMAGE_WEB_PATTERN" \
    --arg proxy_pattern "$IMAGE_PROXY_PATTERN" \
    --arg project "$PROJECT_PATH" \
    '
      .schema == 1
      and (.environment == "test" or .environment == "production")
      and .projectPath == $project
      and (.sha | test($sha_pattern))
      and (.images.api | test($api_pattern))
      and (.images.web | test($web_pattern))
      and (.images.proxy | test($proxy_pattern))
    ' "$file" >/dev/null
}

validate_manifest_for() {
  local file="$1"
  local environment="$2"
  validate_release_manifest "$file"
  jq -e --arg environment "$environment" '.environment == $environment' "$file" >/dev/null
}

validate_production_approval() {
  local file="$1"
  validate_manifest_for "$file" "production"
}

write_release_env() {
  local manifest="$1"
  local destination="$2"
  local temporary="${destination}.tmp"
  umask 077
  {
    printf 'WEDDINGBANK_API_IMAGE=%s\n' "$(jq -r '.images.api' "$manifest")"
    printf 'WEDDINGBANK_WEB_IMAGE=%s\n' "$(jq -r '.images.web' "$manifest")"
    printf 'WEDDINGBANK_PROXY_IMAGE=%s\n' "$(jq -r '.images.proxy' "$manifest")"
  } >"$temporary"
  mv "$temporary" "$destination"
}

write_state() {
  local current="$1"
  local previous="$2"
  local failed="$3"
  local deployment_id="$4"
  local temporary="${STATE_DIR}/state.env.tmp"
  umask 077
  cat >"$temporary" <<EOF
CURRENT_SHA=${current}
PREVIOUS_SHA=${previous}
FAILED_SHA=${failed}
LAST_DEPLOYMENT_ID=${deployment_id}
EOF
  mv "$temporary" "${STATE_DIR}/state.env"
}

write_status() {
  local status="$1"
  local sha="$2"
  local manifest="${3:-}"
  local temporary="${STATE_DIR}/status.json.tmp"
  umask 077
  if [[ -n "$manifest" ]]; then
    jq -n \
      --arg environment "$ENVIRONMENT" \
      --arg status "$status" \
      --arg sha "$sha" \
      --arg recorded_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
      --slurpfile manifest "$manifest" \
      '{
        environment: $environment,
        status: $status,
        sha: $sha,
        recordedAt: $recorded_at,
        images: $manifest[0].images
      }' >"$temporary"
  else
    jq -n \
      --arg environment "$ENVIRONMENT" \
      --arg status "$status" \
      --arg sha "$sha" \
      --arg recorded_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
      '{
        environment: $environment,
        status: $status,
        sha: $sha,
        recordedAt: $recorded_at
      }' >"$temporary"
  fi
  mv "$temporary" "${STATE_DIR}/status.json"
}

usage() {
  cat <<'EOF'
Usage:
  weddingbank-agent.sh --validate-manifest <release-manifest.json>
  weddingbank-agent.sh --validate-approval <production-approval.json>
  weddingbank-agent.sh <test|production> [--once]
EOF
}

if [[ "${1:-}" == "--validate-manifest" ]]; then
  require_command jq
  validate_release_manifest "${2:?manifest path}"
  exit 0
fi

if [[ "${1:-}" == "--validate-approval" ]]; then
  require_command jq
  validate_production_approval "${2:?approval path}"
  exit 0
fi

ENVIRONMENT="${1:-}"
case "$ENVIRONMENT" in
  test | production) ;;
  *)
    usage >&2
    exit 1
    ;;
esac

if [[ "${2:-}" != "" && "${2:-}" != "--once" ]]; then
  usage >&2
  exit 1
fi

require_command jq
require_command curl

if [[ "${WEDDINGBANK_AGENT_TEST:-}" == "1" ]]; then
  [[ -n "${WEDDINGBANK_ROOT:-}" && "${WEDDINGBANK_ROOT}" != "/" ]] || die "Test mode requires WEDDINGBANK_ROOT"
  ROOT="$WEDDINGBANK_ROOT"
  HEALTH_ATTEMPTS="${WEDDINGBANK_HEALTH_ATTEMPTS:-30}"
  HEALTH_INTERVAL="${WEDDINGBANK_HEALTH_INTERVAL:-2}"
else
  [[ -z "${WEDDINGBANK_ROOT:-}" ]] || die "WEDDINGBANK_ROOT is only available in test mode"
  ROOT=""
  HEALTH_ATTEMPTS=30
  HEALTH_INTERVAL=2
fi

case "$ENVIRONMENT" in
  test)
    ENV_FILE="${ROOT}/opt/weddingbank/test/.env"
    COMPOSE_FILE="${ROOT}/opt/weddingbank/test/docker-compose.yml"
    STATE_DIR="${ROOT}/var/lib/weddingbank/test"
    EXPECTED_PROJECT=weddingbank-test
    EXPECTED_PORT=8081
    EXPECTED_REF=develop
    EXPECTED_JOB=deploy:test
    PACKAGE_NAME=weddingbank-test
    ;;
  production)
    ENV_FILE="${ROOT}/opt/weddingbank/prod/.env"
    COMPOSE_FILE="${ROOT}/opt/weddingbank/prod/docker-compose.yml"
    STATE_DIR="${ROOT}/var/lib/weddingbank/prod"
    EXPECTED_PROJECT=weddingbank-prod
    EXPECTED_PORT=8080
    EXPECTED_REF=main
    EXPECTED_JOB=deploy:production
    PACKAGE_NAME=weddingbank-production
    ;;
esac

API_ENV="${ROOT}/opt/weddingbank/deploy/gitlab-api.env"
REGISTRY_ENV="${ROOT}/opt/weddingbank/deploy/gitlab-registry.env"
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

[[ -f "$ENV_FILE" ]] || die "Missing $ENV_FILE"
[[ -f "$COMPOSE_FILE" ]] || die "Missing $COMPOSE_FILE"
[[ -f "$API_ENV" ]] || die "Missing $API_ENV"
[[ -f "$REGISTRY_ENV" ]] || die "Missing $REGISTRY_ENV"

require_command docker
require_command flock

install -d -m 700 "$STATE_DIR"
exec 9>"${STATE_DIR}/deploy.lock"
if ! flock -n 9; then
  log "Deploy already running for ${ENVIRONMENT}"
  exit 0
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
# shellcheck disable=SC1090
source "$API_ENV"
# shellcheck disable=SC1090
source "$REGISTRY_ENV"
set +a

[[ "${COMPOSE_PROJECT_NAME:-}" == "$EXPECTED_PROJECT" ]] || die "Unexpected compose project for ${ENVIRONMENT}"
[[ "${PROXY_BIND_ADDRESS:-}" == "10.120.10.100" ]] || die "Proxy must bind to 10.120.10.100"
[[ "${PROXY_PORT:-}" == "$EXPECTED_PORT" ]] || die "Unexpected proxy port for ${ENVIRONMENT}"
[[ "${HEALTHCHECK_URL:-}" == "http://${PROXY_BIND_ADDRESS}:${PROXY_PORT}/health" ]] || die "Health check URL does not match the proxy"
[[ "${WEDDINGBANK_RUN_MIGRATIONS:-}" == "true" || "${WEDDINGBANK_RUN_MIGRATIONS:-}" == "false" ]] || die "WEDDINGBANK_RUN_MIGRATIONS must be true or false"
[[ -n "${POSTGRES_PASSWORD:-}" ]] || die "POSTGRES_PASSWORD is empty"
case "$POSTGRES_PASSWORD" in
  replace-on-server | ci-not-a-secret | weddingbank_dev)
    die "POSTGRES_PASSWORD is not a server secret"
    ;;
esac
[[ "${GITLAB_API_URL:-}" == "$API_URL" ]] || die "GITLAB_API_URL is not the GitLab.com API"
[[ "${GITLAB_PROJECT_PATH:-}" == "$PROJECT_PATH" ]] || die "GITLAB_PROJECT_PATH is not this project"
[[ "${GITLAB_REGISTRY:-}" == "$REGISTRY" ]] || die "GITLAB_REGISTRY is not the GitLab.com registry"
token_usable "${GITLAB_API_TOKEN:-}" || die "GITLAB_API_TOKEN is not usable"
token_usable "${GITLAB_DEPLOY_TOKEN:-}" || die "GITLAB_DEPLOY_TOKEN is not usable"
token_usable "${GITLAB_DEPLOY_USER:-}" || die "GITLAB_DEPLOY_USER is not usable"

PROJECT_ENCODED="${GITLAB_PROJECT_PATH//\//%2F}"

compose() {
  docker compose \
    --project-name "$COMPOSE_PROJECT_NAME" \
    --env-file "$ENV_FILE" \
    --env-file "${STATE_DIR}/release.env" \
    --file "$COMPOSE_FILE" \
    "$@"
}

health_ok() {
  local attempt
  for attempt in $(seq 1 "$HEALTH_ATTEMPTS"); do
    if curl --fail --silent --show-error --max-time 5 "$HEALTHCHECK_URL" >/dev/null; then
      return 0
    fi
    sleep "$HEALTH_INTERVAL"
  done
  return 1
}

pull_exact() {
  local image="$1"
  docker pull "$image" >/dev/null
  docker image inspect --format '{{range .RepoDigests}}{{println .}}{{end}}' "$image" | grep -Fx "$image" >/dev/null \
    || die "Pulled image does not match the requested digest"
}

registry_login() {
  if ! printf '%s\n' "$GITLAB_DEPLOY_TOKEN" | docker login "$GITLAB_REGISTRY" --username "$GITLAB_DEPLOY_USER" --password-stdin >/dev/null 2>&1; then
    die "Registry login failed"
  fi
}

read_state() {
  current_sha=""
  previous_sha=""
  failed_sha=""
  last_deployment_id=0
  if [[ -f "${STATE_DIR}/state.env" ]]; then
    current_sha="$(awk -F= '$1 == "CURRENT_SHA" { print $2 }' "${STATE_DIR}/state.env")"
    previous_sha="$(awk -F= '$1 == "PREVIOUS_SHA" { print $2 }' "${STATE_DIR}/state.env")"
    failed_sha="$(awk -F= '$1 == "FAILED_SHA" { print $2 }' "${STATE_DIR}/state.env")"
    last_deployment_id="$(awk -F= '$1 == "LAST_DEPLOYMENT_ID" { print $2 }' "${STATE_DIR}/state.env")"
  fi
  [[ -n "${last_deployment_id}" ]] || last_deployment_id=0
}

rollback() {
  write_state "$current_sha" "$previous_sha" "$TARGET_SHA" "$TARGET_ID"
  if [[ ! -f "${STATE_DIR}/previous-release.env" ]]; then
    write_status "failed" "${TARGET_SHA:-unknown}"
    die "Health check failed and no previous release is available"
  fi
  cp "${STATE_DIR}/previous-release.env" "${STATE_DIR}/release.env"
  compose up -d --remove-orphans
  if health_ok; then
    write_status "rolled_back" "${current_sha:-unknown}"
    log "Rolled back ${ENVIRONMENT} to the previous images"
    exit 1
  fi
  write_status "failed" "${TARGET_SHA:-unknown}"
  die "Health check failed and rollback was also unhealthy"
}

deployment_file="$(mktemp)"
manifest_file="$(mktemp)"
trap 'rm -f "$deployment_file" "$manifest_file"' EXIT

curl --fail --silent --show-error \
  --header "PRIVATE-TOKEN: ${GITLAB_API_TOKEN}" \
  --header "Accept: application/json" \
  --output "$deployment_file" \
  "${GITLAB_API_URL}/projects/${PROJECT_ENCODED}/deployments?environment=${ENVIRONMENT}&status=success&order_by=id&sort=desc&per_page=1"

deployment_count="$(jq -r 'if type == "array" then length else -1 end' "$deployment_file")"
[[ "$deployment_count" -ge 0 ]] || die "Deployments response is not a list"
if [[ "$deployment_count" -eq 0 ]]; then
  log "No successful ${ENVIRONMENT} deployment is available"
  exit 0
fi

TARGET_ID="$(jq -r '.[0].id' "$deployment_file")"
TARGET_SHA="$(jq -r '.[0].sha' "$deployment_file")"
deployment_status="$(jq -r '.[0].status' "$deployment_file")"
deployment_environment="$(jq -r '.[0].environment.name' "$deployment_file")"
deployment_ref="$(jq -r '.[0].ref' "$deployment_file")"
deployment_job="$(jq -r '.[0].deployable.name' "$deployment_file")"

[[ "$TARGET_ID" =~ ^[0-9]+$ ]] || die "Deployment id is not numeric"
[[ "$deployment_status" == "success" ]] || die "Deployment is not successful"
[[ "$deployment_environment" == "$ENVIRONMENT" ]] || die "Deployment environment does not match ${ENVIRONMENT}"
[[ "$deployment_ref" == "$EXPECTED_REF" ]] || die "Deployment ref does not match ${EXPECTED_REF}"
[[ "$deployment_job" == "$EXPECTED_JOB" ]] || die "Deployment job does not match ${EXPECTED_JOB}"
[[ "$TARGET_SHA" =~ $SHA_PATTERN ]] || die "Deployment SHA is invalid"

read_state
if [[ "$TARGET_ID" -le "$last_deployment_id" ]]; then
  log "Deployment ${TARGET_ID} is not newer than ${last_deployment_id} for ${ENVIRONMENT}"
  exit 0
fi
if [[ -n "$failed_sha" && "$failed_sha" == "$TARGET_SHA" ]]; then
  write_state "$current_sha" "$previous_sha" "$failed_sha" "$TARGET_ID"
  log "Not retrying failed ${ENVIRONMENT} release ${TARGET_SHA}"
  exit 0
fi
if [[ "$current_sha" == "$TARGET_SHA" ]]; then
  write_state "$current_sha" "$previous_sha" "" "$TARGET_ID"
  log "${ENVIRONMENT} is already running ${TARGET_SHA}"
  exit 0
fi

curl --fail --silent --show-error \
  --header "DEPLOY-TOKEN: ${GITLAB_DEPLOY_TOKEN}" \
  --output "$manifest_file" \
  "${GITLAB_API_URL}/projects/${PROJECT_ENCODED}/packages/generic/${PACKAGE_NAME}/${TARGET_SHA}/manifest.json" \
  || die "Release manifest download failed"

validate_manifest_for "$manifest_file" "$ENVIRONMENT" || die "Invalid release manifest"
[[ "$(jq -r '.sha' "$manifest_file")" == "$TARGET_SHA" ]] || die "Manifest SHA does not match the deployment"
[[ "$(jq -r '.projectPath' "$manifest_file")" == "$PROJECT_PATH" ]] || die "Manifest project does not match"

log "Selected ${ENVIRONMENT} deployment ${TARGET_ID} for ${TARGET_SHA}"
registry_login
pull_exact "$(jq -r '.images.api' "$manifest_file")"
pull_exact "$(jq -r '.images.web' "$manifest_file")"
pull_exact "$(jq -r '.images.proxy' "$manifest_file")"

if [[ -f "${STATE_DIR}/release.env" ]]; then
  cp "${STATE_DIR}/release.env" "${STATE_DIR}/previous-release.env"
fi
write_release_env "$manifest_file" "${STATE_DIR}/release.env"

if [[ "$WEDDINGBANK_RUN_MIGRATIONS" == "true" ]]; then
  if [[ "$ENVIRONMENT" == "production" ]]; then
    bash "${SCRIPT_DIR}/weddingbank-backup.sh" production pre-migrate
  fi
  compose config --services | grep -qx migrate || die "Migrations are enabled but the compose file has no migrate service"
  compose run --rm --no-deps migrate
else
  log "Database migrations are disabled"
fi

compose up -d --remove-orphans
if ! health_ok; then
  log "Health check failed for ${TARGET_SHA}"
  rollback
fi

write_state "$TARGET_SHA" "$current_sha" "" "$TARGET_ID"
write_status "succeeded" "$TARGET_SHA" "$manifest_file"
log "Deployed ${ENVIRONMENT} ${TARGET_SHA}"
