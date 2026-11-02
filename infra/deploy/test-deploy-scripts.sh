#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
agent="${root}/infra/deploy/weddingbank-agent.sh"
backup="${root}/infra/deploy/weddingbank-backup.sh"
publish="${root}/infra/deploy/publish-manifest.sh"
workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT

if grep -Ei 'github|ghcr\.io' "$agent" "$publish" "$root/infra/deploy/record-image-digests.sh"; then
  echo "deploy scripts still reference GitHub" >&2
  exit 1
fi

sha="0123456789abcdef0123456789abcdef01234567"
other_sha="fedcba9876543210fedcba9876543210fedcba98"
api="registry.gitlab.com/evsb-group/weddingbank/api@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
web="registry.gitlab.com/evsb-group/weddingbank/web@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
proxy="registry.gitlab.com/evsb-group/weddingbank/proxy@sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
old_api="registry.gitlab.com/evsb-group/weddingbank/api@sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"
old_web="registry.gitlab.com/evsb-group/weddingbank/web@sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"
old_proxy="registry.gitlab.com/evsb-group/weddingbank/proxy@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
api_token="read-api-token-value"
deploy_token="deploy-token-value"
deploy_user="gitlab+deploy-token-1"

manifest() {
  local destination="$1"
  local environment="$2"
  local commit="$3"
  local api_image="$4"
  local web_image="$5"
  local proxy_image="$6"
  jq -n \
    --arg environment "$environment" \
    --arg sha "$commit" \
    --arg api "$api_image" \
    --arg web "$web_image" \
    --arg proxy "$proxy_image" \
    '{schema: 1, environment: $environment, projectPath: "evsb-group/weddingbank", sha: $sha, images: {api: $api, web: $web, proxy: $proxy}}' \
    >"$destination"
}

deployment() {
  local destination="$1"
  local id="$2"
  local environment="$3"
  local ref="$4"
  local job="$5"
  local commit="$6"
  jq -n \
    --argjson id "$id" \
    --arg environment "$environment" \
    --arg ref "$ref" \
    --arg job "$job" \
    --arg sha "$commit" \
    '[{id: $id, status: "success", ref: $ref, sha: $sha, environment: {name: $environment}, deployable: {name: $job}}]' \
    >"$destination"
}

manifest "$workdir/valid-test.json" test "$sha" "$api" "$web" "$proxy"
bash "$agent" --validate-manifest "$workdir/valid-test.json"
manifest "$workdir/valid-prod.json" production "$sha" "$api" "$web" "$proxy"
bash "$agent" --validate-approval "$workdir/valid-prod.json"

if bash "$agent" --validate-approval "$workdir/valid-test.json"; then
  echo "test manifest was accepted as production approval" >&2
  exit 1
fi

jq 'del(.projectPath)' "$workdir/valid-test.json" >"$workdir/incomplete.json"
if bash "$agent" --validate-manifest "$workdir/incomplete.json"; then
  echo "incomplete manifest was accepted" >&2
  exit 1
fi

jq '.images.api = "registry.gitlab.com/evsb-group/weddingbank/api:latest"' "$workdir/valid-test.json" >"$workdir/latest.json"
if bash "$agent" --validate-manifest "$workdir/latest.json"; then
  echo "latest tag was accepted" >&2
  exit 1
fi

jq '.images.web = "ghcr.io/example/weddingbank-web@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"' "$workdir/valid-test.json" >"$workdir/registry.json"
if bash "$agent" --validate-manifest "$workdir/registry.json"; then
  echo "unexpected registry was accepted" >&2
  exit 1
fi

jq '.projectPath = "other/project"' "$workdir/valid-test.json" >"$workdir/project.json"
if bash "$agent" --validate-manifest "$workdir/project.json"; then
  echo "unexpected project was accepted" >&2
  exit 1
fi

CI_COMMIT_SHA="$sha" \
CI_COMMIT_BRANCH="develop" \
CI_REGISTRY_IMAGE="registry.gitlab.com/evsb-group/weddingbank" \
WEDDINGBANK_API_DIGEST="sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" \
WEDDINGBANK_WEB_DIGEST="sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" \
WEDDINGBANK_PROXY_DIGEST="sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc" \
WEDDINGBANK_PUBLISH_DRY_RUN=1 \
WEDDINGBANK_MANIFEST_OUT="$workdir/published.json" \
  bash "$publish" test
cmp -s "$workdir/valid-test.json" "$workdir/published.json"

if CI_COMMIT_SHA="$sha" \
  CI_COMMIT_BRANCH="develop" \
  CI_REGISTRY_IMAGE="registry.gitlab.com/evsb-group/weddingbank" \
  WEDDINGBANK_API_DIGEST="sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" \
  WEDDINGBANK_WEB_DIGEST="sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" \
  WEDDINGBANK_PROXY_DIGEST="sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc" \
  WEDDINGBANK_PUBLISH_DRY_RUN=1 \
  bash "$publish" production
then
  echo "production manifest was published from develop" >&2
  exit 1
fi

bindir="${workdir}/bin"
stub="${workdir}/stub"
mkdir -p "$bindir" "$stub"

cat >"${bindir}/curl" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
output=""
url=""
auth=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output)
      output="$2"
      shift 2
      ;;
    --max-time)
      shift 2
      ;;
    --header)
      name="${2%%:*}"
      case "$name" in
        PRIVATE-TOKEN) auth="api" ;;
        DEPLOY-TOKEN) auth="deploy" ;;
        *) ;;
      esac
      shift 2
      ;;
    --fail | --silent | --show-error | --location)
      shift
      ;;
    http://* | https://*)
      url="$1"
      shift
      ;;
    *)
      shift
      ;;
  esac
done
printf '%s %s\n' "$auth" "$url" >>"${WEDDINGBANK_STUB_DIR}/requests"
if [[ "$url" == *"/health" ]]; then
  count=0
  if [[ -f "${WEDDINGBANK_STUB_DIR}/health.count" ]]; then
    count="$(cat "${WEDDINGBANK_STUB_DIR}/health.count")"
  fi
  count=$((count + 1))
  printf '%s\n' "$count" >"${WEDDINGBANK_STUB_DIR}/health.count"
  if [[ "$count" -le "${WEDDINGBANK_HEALTH_FAILS:-0}" ]]; then
    exit 22
  fi
  exit 0
fi
if [[ "$url" == *"/deployments?"* ]]; then
  [[ "$auth" == "api" ]] || exit 22
  if [[ -n "$output" ]]; then
    cp "${WEDDINGBANK_STUB_DIR}/deployments.json" "$output"
  else
    cat "${WEDDINGBANK_STUB_DIR}/deployments.json"
  fi
  exit 0
fi
if [[ "$url" == *"/packages/generic/"* ]]; then
  [[ "$auth" == "deploy" ]] || exit 22
  relative="${url#*"/packages/generic/"}"
  source="${WEDDINGBANK_STUB_DIR}/packages/${relative}"
  [[ -f "$source" ]] || exit 22
  if [[ -n "$output" ]]; then
    cp "$source" "$output"
  else
    cat "$source"
  fi
  exit 0
fi
exit 22
EOF

cat >"${bindir}/docker" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
if [[ "${1:-}" == "login" ]]; then
  cat >/dev/null
  if [[ "${WEDDINGBANK_STUB_LOGIN_FAIL:-}" == "1" ]]; then
    exit 1
  fi
  exit 0
fi
if [[ "${1:-}" == "pull" ]]; then
  printf '%s\n' "$2" >>"${WEDDINGBANK_STUB_DIR}/pulls"
  exit 0
fi
if [[ "${1:-}" == "image" && "${2:-}" == "inspect" ]]; then
  printf '%s\n' "${!#}"
  exit 0
fi
if [[ "${1:-}" == "compose" ]]; then
  printf '%s\n' "$*" >>"${WEDDINGBANK_STUB_DIR}/compose"
  exit 0
fi
exit 1
EOF
chmod +x "${bindir}/curl" "${bindir}/docker"

install_env() {
  local base="$1"
  local environment="$2"
  local project port
  if [[ "$environment" == "test" ]]; then
    project="weddingbank-test"
    port="8081"
  else
    project="weddingbank-prod"
    port="8080"
  fi
  mkdir -p "${base}/opt/weddingbank/${environment}" "${base}/opt/weddingbank/deploy" "${base}/opt/weddingbank/prod"
  if [[ "$environment" == "production" ]]; then
    mkdir -p "${base}/opt/weddingbank/prod"
  fi
  cat >"${base}/opt/weddingbank/${environment}/.env" <<EOF
COMPOSE_PROJECT_NAME=${project}
PROXY_BIND_ADDRESS=10.120.10.100
PROXY_PORT=${port}
HEALTHCHECK_URL=http://10.120.10.100:${port}/health
PUBLIC_ORIGIN=https://example.invalid
POSTGRES_USER=weddingbank_${environment}
POSTGRES_PASSWORD=server-secret-value
POSTGRES_DB=weddingbank_${environment}
WEDDINGBANK_RUN_MIGRATIONS=false
EOF
  printf 'services: {}\n' >"${base}/opt/weddingbank/${environment}/docker-compose.yml"
  cat >"${base}/opt/weddingbank/deploy/gitlab-api.env" <<EOF
GITLAB_API_URL=https://gitlab.com/api/v4
GITLAB_PROJECT_PATH=evsb-group/weddingbank
GITLAB_API_TOKEN=${api_token}
EOF
  cat >"${base}/opt/weddingbank/deploy/gitlab-registry.env" <<EOF
GITLAB_REGISTRY=registry.gitlab.com
GITLAB_DEPLOY_USER=${deploy_user}
GITLAB_DEPLOY_TOKEN=${deploy_token}
EOF
}

store_package() {
  local environment="$1"
  local commit="$2"
  local file="$3"
  local package="weddingbank-${environment}"
  if [[ "$environment" == "production" ]]; then
    package="weddingbank-production"
  fi
  mkdir -p "${stub}/packages/${package}/${commit}"
  cp "$file" "${stub}/packages/${package}/${commit}/manifest.json"
}

run_agent() {
  local environment="$1"
  local base="$2"
  shift 2
  env -u WEDDINGBANK_STUB_LOGIN_FAIL \
    PATH="${bindir}:${PATH}" \
    WEDDINGBANK_AGENT_TEST=1 \
    WEDDINGBANK_ROOT="$base" \
    WEDDINGBANK_STUB_DIR="$stub" \
    WEDDINGBANK_HEALTH_ATTEMPTS=1 \
    WEDDINGBANK_HEALTH_INTERVAL=0 \
    WEDDINGBANK_HEALTH_FAILS="${WEDDINGBANK_HEALTH_FAILS:-0}" \
    WEDDINGBANK_STUB_LOGIN_FAIL="${WEDDINGBANK_STUB_LOGIN_FAIL:-}" \
    bash "$agent" "$environment" --once
}

assert_secret_free() {
  local file="$1"
  if grep -F "$api_token" "$file" || grep -F "$deploy_token" "$file"; then
    echo "credentials were written to the agent output" >&2
    exit 1
  fi
}

test_root="${workdir}/root"
install_env "$test_root" test
manifest "$workdir/old.json" test "$other_sha" "$old_api" "$old_web" "$old_proxy"
manifest "$workdir/new.json" test "$sha" "$api" "$web" "$proxy"
store_package test "$other_sha" "$workdir/old.json"
store_package test "$sha" "$workdir/new.json"
deployment "$stub/deployments.json" 4 test develop deploy:test "$other_sha"
: >"${stub}/requests"
: >"${stub}/pulls"
: >"${stub}/compose"
rm -f "${stub}/health.count"
WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-old.log" 2>&1
assert_secret_free "${workdir}/run-old.log"
grep -q "^api .*environment=test&status=success&order_by=id&sort=desc&per_page=1$" "${stub}/requests"
grep -q "^deploy .*/weddingbank-test/${other_sha}/manifest.json" "${stub}/requests"
if grep -q "weddingbank-production" "${stub}/requests"; then
  echo "test agent requested a production package" >&2
  exit 1
fi

deployment "$stub/deployments.json" 3 test develop deploy:test "$sha"
: >"${stub}/pulls"
WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-old-id.log" 2>&1
if [[ -s "${stub}/pulls" ]]; then
  echo "older deployment was applied" >&2
  exit 1
fi

deployment "$stub/deployments.json" 9 production main deploy:production "$sha"
if WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-cross.log" 2>&1; then
  echo "production deployment was applied by the test agent" >&2
  exit 1
fi
assert_secret_free "${workdir}/run-cross.log"
if grep -q "weddingbank-production" "${stub}/requests"; then
  echo "cross-environment package was requested" >&2
  exit 1
fi

deployment "$stub/deployments.json" 5 test develop deploy:test "$sha"
store_package test "$sha" "$workdir/valid-prod.json"
if WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-bad-manifest.log" 2>&1; then
  echo "production manifest was applied to test" >&2
  exit 1
fi
assert_secret_free "${workdir}/run-bad-manifest.log"
store_package test "$sha" "$workdir/new.json"

mkdir -p "${test_root}/var/lib/weddingbank/test"
cat >"${test_root}/var/lib/weddingbank/test/release.env" <<EOF
WEDDINGBANK_API_IMAGE=${old_api}
WEDDINGBANK_WEB_IMAGE=${old_web}
WEDDINGBANK_PROXY_IMAGE=${old_proxy}
EOF
cat >"${test_root}/var/lib/weddingbank/test/state.env" <<EOF
CURRENT_SHA=${other_sha}
PREVIOUS_SHA=
FAILED_SHA=
LAST_DEPLOYMENT_ID=4
EOF
rm -f "${stub}/health.count"
: >"${stub}/pulls"
: >"${stub}/compose"
WEDDINGBANK_HEALTH_FAILS=1 run_agent test "$test_root" >"${workdir}/run-rollback.log" 2>&1 || rollback_code=$?
rollback_code="${rollback_code:-0}"
[[ "$rollback_code" -eq 1 ]] || {
  echo "failed health check exited ${rollback_code}" >&2
  exit 1
}
assert_secret_free "${workdir}/run-rollback.log"
grep -q "$old_api" "${test_root}/var/lib/weddingbank/test/release.env"
if grep -q "$api" "${test_root}/var/lib/weddingbank/test/release.env"; then
  echo "rollback left the failed image in place" >&2
  exit 1
fi
grep -q "rolled_back" "${test_root}/var/lib/weddingbank/test/status.json"

pulls_before="$(wc -l <"${stub}/pulls" | tr -d ' ')"
WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-retry.log" 2>&1
pulls_after="$(wc -l <"${stub}/pulls" | tr -d ' ')"
[[ "$pulls_before" == "$pulls_after" ]] || {
  echo "failed deployment was retried" >&2
  exit 1
}
assert_secret_free "${workdir}/run-retry.log"

deployment "$stub/deployments.json" 6 test develop deploy:test "$sha"
WEDDINGBANK_HEALTH_FAILS=0 run_agent test "$test_root" >"${workdir}/run-same-sha.log" 2>&1
pulls_same="$(wc -l <"${stub}/pulls" | tr -d ' ')"
[[ "$pulls_same" == "$pulls_after" ]] || {
  echo "failed SHA was deployed again under a new id" >&2
  exit 1
}

WEDDINGBANK_STUB_LOGIN_FAIL=1
deployment "$stub/deployments.json" 7 test develop deploy:test "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
manifest "$workdir/login.json" test "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" "$api" "$web" "$proxy"
store_package test "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" "$workdir/login.json"
if WEDDINGBANK_HEALTH_FAILS=0 WEDDINGBANK_STUB_LOGIN_FAIL=1 run_agent test "$test_root" >"${workdir}/run-login.log" 2>&1; then
  echo "registry login failure was ignored" >&2
  exit 1
fi
assert_secret_free "${workdir}/run-login.log"
grep -q "Registry login failed" "${workdir}/run-login.log"

if bash "$backup" test daily; then
  echo "test environment was allowed to create a production backup" >&2
  exit 1
fi

if [[ -d /opt/weddingbank ]]; then
  echo "/opt/weddingbank exists; deploy invocation was not tested" >&2
  exit 1
fi

if bash "$agent" test --once; then
  echo "agent continued without server configuration" >&2
  exit 1
fi

echo "Deploy script checks passed"
