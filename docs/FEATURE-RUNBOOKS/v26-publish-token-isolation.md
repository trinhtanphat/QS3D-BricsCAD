# V26 publish-token isolation

## Purpose

The V26 commercial release lane must keep repository write authority away from the self-hosted BricsCAD qualification runner. Source checkout, source guards, Core/V26 build, package construction, Authenticode signing and licensed runtime validation require repository read access only. Only the final hosted GitHub Release transaction requires `contents: write`.

## Required workflow boundary

1. Workflow default permissions are `contents: read`.
2. A self-hosted `qualify` job runs all source/build/sign/runtime qualification with explicit `contents: read`.
3. The qualification job freezes the exact release candidate into an Actions artifact after checksum/update-manifest creation and runtime/signing gates.
4. A separate `windows-latest` `release` job depends on `qualify`, checks out the exact `${{ github.sha }}` with `persist-credentials: false`, downloads the frozen candidate, revalidates its immutable identity, and alone receives `contents: write`.
5. The hosted publisher must not rebuild or re-sign the package. It only verifies and publishes the candidate already qualified by the self-hosted job.

## Invariants to preserve

The split must preserve strict SemVer/prerelease/stable validation, exact lightweight tag-to-workflow-SHA binding, V26-only package identity, optional unsigned prerelease behavior, mandatory stable signing/runtime rules, package checksum integrity, signed update-manifest behavior, draft transaction ownership, ambiguous acknowledgement recovery, uploaded-byte verification, exact asset identity/size/hash checks, publish-response reconciliation, and automatic rollback only for transaction-owned draft/tag state.

Hosted/static evidence is not licensed BricsCAD `LOCAL_PASS`. This package changes workflow authority boundaries only; it does not claim or replace native V26 runtime qualification.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-publish-token-isolation.py
python scripts/preflight-v26-package-release.py
python scripts/preflight-all.py
```

The focused guard fails closed if workflow-wide write authority returns, if the self-hosted qualification job gains `contents: write`/`GITHUB_TOKEN` publication access, if the write-capable job moves to self-hosted infrastructure, or if publication occurs before the frozen qualification artifact crosses the job boundary.
