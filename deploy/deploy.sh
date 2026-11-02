#!/usr/bin/env bash
# Deploy The Wedding Bank on the VM that already runs the self-hosted runner.
# Used by GitHub Actions, or by hand from a checkout of this repository.
# Modeled on AmbuWear / EhboStock / Onsy deploy/deploy.sh.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

resolve_deploy_dir() {
  if [[ -n "${WEDDINGBANK_DEPLOY_DIR:-}" ]]; then
    printf '%s\n' "${WEDDINGBANK_DEPLOY_DIR}"
    return 0
  fi

  local candidate
  for candidate in \
    "${HOME}/config/weddingbank" \
    "/home/administrator/config/weddingbank" \
    "${HOME}/Weddingbank"
  do
    if [[ -f "${candidate}/docker-compose.yml" ]]; then
      printf '%s\n' "${candidate}"
      return 0
    fi
  done
  for candidate in \
    "${HOME}/config/weddingbank" \
    "/home/administrator/config/weddingbank" \
    "${HOME}/Weddingbank"
  do
    if [[ -d "${candidate}" ]]; then
      printf '%s\n' "${candidate}"
      return 0
    fi
  done

  if [[ "${REPO_ROOT}" == *"/_work/"* ]]; then
    echo "No production deploy directory found under ~/config/weddingbank or ~/Weddingbank." >&2
    echo "On the VM, create the directory once and put secrets there (not in the Actions _work checkout):" >&2
    echo "  mkdir -p ~/config/weddingbank" >&2
    echo "  cp /path/to/.env.example ~/config/weddingbank/.env" >&2
    echo "Or set WEDDINGBANK_DEPLOY_DIR / GitHub secret DEPLOY_PATH to the production path." >&2
    exit 1
  fi

  printf '%s\n' "${REPO_ROOT}"
}

DEPLOY_DIR="$(resolve_deploy_dir)"
BRANCH="${WEDDINGBANK_DEPLOY_BRANCH:-}"
SKIP_GIT_PULL="${WEDDINGBANK_SKIP_GIT_PULL:-0}"
HEALTH_URL="${WEDDINGBANK_HEALTH_URL:-http://10.120.10.100:8080/health}"
if [[ -z "${COMPOSE_CMD:-}" ]]; then
  if command -v docker-compose >/dev/null 2>&1; then
    COMPOSE_CMD="docker-compose"
  else
    COMPOSE_CMD="docker compose"
  fi
fi
COMPOSE_FILES="-f docker-compose.yml -f docker-compose.prod.yml"
SERVICES="postgres api web proxy"

if [[ ! -d "${DEPLOY_DIR}" ]]; then
  mkdir -p "${DEPLOY_DIR}"
fi

echo "==> Deploy directory: ${DEPLOY_DIR}"
echo "==> Script / checkout root: ${REPO_ROOT}"

sync_checkout_into_deploy_dir() {
  local src="$1"
  local dst="$2"
  echo "==> Syncing Actions/checkout ${src} -> ${dst} (preserving .env)"
  if ! command -v rsync >/dev/null 2>&1; then
    echo "Error: rsync is required to sync the Actions checkout into the deploy directory." >&2
    exit 1
  fi
  mkdir -p "${dst}"
  rsync -a --delete \
    --exclude '.env' \
    --exclude '.env.local' \
    --exclude 'node_modules' \
    --exclude 'dist' \
    "${src}/" "${dst}/"
  echo "==> Sync complete. HEAD in deploy dir: $(git -C "${dst}" rev-parse --short HEAD 2>/dev/null || echo unknown)"
}

checkout_can_sync() {
  [[ -f "${REPO_ROOT}/docker-compose.yml" && -f "${REPO_ROOT}/infra/docker/api.Dockerfile" ]]
}

if [[ "${SKIP_GIT_PULL}" == "1" ]]; then
  echo "==> Skipping source sync (WEDDINGBANK_SKIP_GIT_PULL=1)"
elif [[ "${REPO_ROOT}" != "${DEPLOY_DIR}" ]] && checkout_can_sync; then
  sync_checkout_into_deploy_dir "${REPO_ROOT}" "${DEPLOY_DIR}"
elif git -C "${DEPLOY_DIR}" rev-parse --is-inside-work-tree >/dev/null 2>&1 && git -C "${DEPLOY_DIR}" remote get-url origin >/dev/null 2>&1; then
  cd "${DEPLOY_DIR}"
  if [[ -z "${BRANCH}" ]]; then
    BRANCH="$(git symbolic-ref --short HEAD 2>/dev/null || echo main)"
  fi
  echo "==> Syncing ${DEPLOY_DIR} to origin/${BRANCH} via git"
  echo "==> HEAD before: $(git rev-parse --short HEAD 2>/dev/null || echo unknown)"
  if ! git fetch origin "${BRANCH}" --tags; then
    echo "Error: git fetch origin ${BRANCH} --tags failed." >&2
    echo "Tip: run deploy from GitHub Actions so the checkout is synced without a deploy key." >&2
    exit 1
  fi
  if ! git rev-parse --verify "origin/${BRANCH}" >/dev/null 2>&1; then
    echo "Error: origin/${BRANCH} not found after fetch." >&2
    exit 1
  fi
  git checkout "${BRANCH}"
  git reset --hard "origin/${BRANCH}"
  expected_sha="$(git rev-parse "origin/${BRANCH}")"
  actual_sha="$(git rev-parse HEAD)"
  if [[ "${expected_sha}" != "${actual_sha}" ]]; then
    echo "Error: HEAD ${actual_sha} != origin/${BRANCH} ${expected_sha}" >&2
    exit 1
  fi
  echo "==> HEAD after:  $(git rev-parse --short HEAD) (matches origin/${BRANCH})"
elif [[ "${REPO_ROOT}" != "${DEPLOY_DIR}" ]]; then
  echo "Error: cannot sync Actions checkout into ${DEPLOY_DIR} (missing docker-compose.yml or infra/docker/api.Dockerfile)." >&2
  exit 1
else
  echo "Error: no Actions checkout to sync and ${DEPLOY_DIR} cannot git fetch -- refusing stale deploy." >&2
  exit 1
fi

cd "${DEPLOY_DIR}"

if [[ ! -f "docker-compose.yml" ]]; then
  echo "No docker-compose.yml in ${DEPLOY_DIR}" >&2
  exit 1
fi

if [[ ! -f ".env" ]]; then
  echo "Missing ${DEPLOY_DIR}/.env -- create it once on the server deploy path (not in Actions _work):" >&2
  echo "  cp ${DEPLOY_DIR}/.env.example ${DEPLOY_DIR}/.env" >&2
  echo "  # then set POSTGRES_PASSWORD and RESEND_API_KEY" >&2
  exit 1
fi

echo "==> Freeing Docker disk space before build"
df -h / /var/lib/docker 2>/dev/null || df -h / || true
if command -v docker >/dev/null 2>&1; then
  docker system df 2>/dev/null || true
  avail_kb="$(df -Pk / | awk 'NR==2 {print $4}')"
  avail_gb="$((avail_kb / 1024 / 1024))"
  echo "==> Free space on /: ${avail_gb}G"
  prune_with_timeout() {
    local desc="$1"
    shift
    echo "==> ${desc}"
    if command -v timeout >/dev/null 2>&1; then
      timeout 120 "$@" 2>/dev/null && return 0
      local rc=$?
      if [[ "${rc}" -eq 124 ]]; then
        echo "Warning: ${desc} timed out after 120s — continuing" >&2
      else
        echo "Warning: ${desc} exited ${rc} — continuing" >&2
      fi
    else
      "$@" 2>/dev/null || echo "Warning: ${desc} failed — continuing" >&2
    fi
    return 0
  }
  # Never prune volumes — postgres and data-protection keys live there.
  if [[ "${avail_gb}" -gt 15 ]]; then
    echo "==> Skipping aggressive prune (>15G free)"
    prune_with_timeout "container prune" docker container prune -f
  else
    echo "==> Low disk (${avail_gb}G free) — running timed prunes"
    prune_with_timeout "builder prune" docker builder prune -af
    prune_with_timeout "image prune" docker image prune -af
    prune_with_timeout "container prune" docker container prune -f
  fi
else
  echo "Warning: docker CLI not found -- skipping prune" >&2
fi

echo "==> Compose: ${COMPOSE_CMD} ${COMPOSE_FILES}"
echo "==> Stopping existing containers (volumes preserved)"
${COMPOSE_CMD} ${COMPOSE_FILES} down
echo "==> Building and starting: ${SERVICES}"
${COMPOSE_CMD} ${COMPOSE_FILES} up -d --build ${SERVICES}

echo "==> Health check: ${HEALTH_URL}"
response=""
for attempt in $(seq 1 30); do
  if response="$(curl -sf "${HEALTH_URL}" 2>/dev/null)" && echo "${response}" | grep -q 'Healthy'; then
    echo "Health check passed (attempt ${attempt})"
    echo "${response}"
    exit 0
  fi
  echo "Waiting for Wedding Bank... (${attempt}/30)"
  sleep 5
done

echo "Health check failed after 30 attempts" >&2
${COMPOSE_CMD} ${COMPOSE_FILES} ps
exit 1
