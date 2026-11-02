#!/usr/bin/env bash
# Push the three images and record the registry digests for the deploy job.
set -euo pipefail

[[ -n "${CI_REGISTRY_IMAGE:-}" && -n "${CI_COMMIT_SHA:-}" ]] || {
  echo "CI_REGISTRY_IMAGE and CI_COMMIT_SHA are required." >&2
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

output="${CI_PROJECT_DIR:-.}/image-digests.env"
: >"$output"

record() {
  local name="$1"
  local variable="$2"
  local ref="${CI_REGISTRY_IMAGE}/${name}:${CI_COMMIT_SHA}"
  local push_output digest
  push_output="$(docker push "$ref" 2>&1)" || {
    printf '%s\n' "$push_output" >&2
    exit 1
  }
  printf '%s\n' "$push_output" >&2
  digest="$(printf '%s\n' "$push_output" | sed -n 's/.*digest: \(sha256:[a-f0-9]\{64\}\).*/\1/p' | tail -n 1)"
  if [[ -z "$digest" ]]; then
    digest="$(docker image inspect --format '{{range .RepoDigests}}{{println .}}{{end}}' "$ref" \
      | sed -n "s|^${CI_REGISTRY_IMAGE}/${name}@\\(sha256:[a-f0-9]\\{64\\}\\)$|\\1|p" \
      | head -n 1)"
  fi
  [[ "$digest" =~ ^sha256:[a-f0-9]{64}$ ]] || {
    echo "No registry digest for ${name}." >&2
    exit 1
  }
  printf '%s=%s\n' "$variable" "$digest" >>"$output"
}

record api WEDDINGBANK_API_DIGEST
record web WEDDINGBANK_WEB_DIGEST
record proxy WEDDINGBANK_PROXY_DIGEST
