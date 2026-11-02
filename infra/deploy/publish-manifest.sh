#!/usr/bin/env bash
# Upload the immutable release manifest. This does not contact the Debian server.
set -euo pipefail

environment="${1:-}"
case "$environment" in
  test)
    package="weddingbank-test"
    expected_branch="develop"
    ;;
  production)
    package="weddingbank-production"
    expected_branch="main"
    ;;
  *)
    echo "Usage: publish-manifest.sh <test|production>" >&2
    exit 1
    ;;
esac

require() {
  [[ -n "${1:-}" ]] || {
    echo "Missing CI value for the release manifest." >&2
    exit 1
  }
}

require "${CI_COMMIT_SHA:-}"
require "${CI_COMMIT_BRANCH:-}"
require "${CI_REGISTRY_IMAGE:-}"
require "${WEDDINGBANK_API_DIGEST:-}"
require "${WEDDINGBANK_WEB_DIGEST:-}"
require "${WEDDINGBANK_PROXY_DIGEST:-}"

[[ "$CI_COMMIT_BRANCH" == "$expected_branch" ]] || {
  echo "Refusing to publish a ${environment} manifest from ${CI_COMMIT_BRANCH}." >&2
  exit 1
}
[[ "$CI_REGISTRY_IMAGE" == "registry.gitlab.com/evsb-group/weddingbank" ]] || {
  echo "CI_REGISTRY_IMAGE is not the Weddingbank registry." >&2
  exit 1
}
[[ "$CI_COMMIT_SHA" =~ ^[0-9a-f]{40}$ ]] || {
  echo "CI_COMMIT_SHA is not a full commit id." >&2
  exit 1
}

digest_ok() {
  [[ "$1" =~ ^sha256:[a-f0-9]{64}$ ]]
}
digest_ok "$WEDDINGBANK_API_DIGEST" || exit 1
digest_ok "$WEDDINGBANK_WEB_DIGEST" || exit 1
digest_ok "$WEDDINGBANK_PROXY_DIGEST" || exit 1

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
manifest="$(mktemp)"
trap 'rm -f "$manifest"' EXIT

jq -n \
  --arg environment "$environment" \
  --arg sha "$CI_COMMIT_SHA" \
  --arg api "${CI_REGISTRY_IMAGE}/api@${WEDDINGBANK_API_DIGEST}" \
  --arg web "${CI_REGISTRY_IMAGE}/web@${WEDDINGBANK_WEB_DIGEST}" \
  --arg proxy "${CI_REGISTRY_IMAGE}/proxy@${WEDDINGBANK_PROXY_DIGEST}" \
  '{
    schema: 1,
    environment: $environment,
    projectPath: "evsb-group/weddingbank",
    sha: $sha,
    images: {api: $api, web: $web, proxy: $proxy}
  }' >"$manifest"

bash "${root}/infra/deploy/weddingbank-agent.sh" --validate-manifest "$manifest"
if [[ "$environment" == "production" ]]; then
  bash "${root}/infra/deploy/weddingbank-agent.sh" --validate-approval "$manifest"
fi

if [[ "${WEDDINGBANK_PUBLISH_DRY_RUN:-}" == "1" ]]; then
  if [[ -n "${WEDDINGBANK_MANIFEST_OUT:-}" ]]; then
    cp "$manifest" "$WEDDINGBANK_MANIFEST_OUT"
  fi
  exit 0
fi

require "${CI_JOB_TOKEN:-}"
require "${CI_API_V4_URL:-}"
require "${CI_PROJECT_ID:-}"

url="${CI_API_V4_URL}/projects/${CI_PROJECT_ID}/packages/generic/${package}/${CI_COMMIT_SHA}/manifest.json"
body="$(mktemp)"
trap 'rm -f "$manifest" "$body"' EXIT
status="$(curl --silent --show-error --output "$body" --write-out '%{http_code}' \
  --header "JOB-TOKEN: ${CI_JOB_TOKEN}" \
  --upload-file "$manifest" \
  "$url")"

case "$status" in
  200 | 201)
    exit 0
    ;;
  400)
    existing="$(mktemp)"
    trap 'rm -f "$manifest" "$body" "$existing"' EXIT
    curl --fail --silent --show-error \
      --header "JOB-TOKEN: ${CI_JOB_TOKEN}" \
      --output "$existing" \
      "$url"
    cmp -s "$manifest" "$existing" || {
      echo "Existing manifest does not match this release." >&2
      exit 1
    }
    ;;
  *)
    echo "Manifest upload failed with HTTP ${status}." >&2
    exit 1
    ;;
esac
