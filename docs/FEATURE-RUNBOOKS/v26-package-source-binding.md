# V26 package source binding

Lane-Key: `issue-5217`

## Purpose

The V26 release candidate must carry the same exact Git source identity as the workflow run that built and qualified it. `PACKAGE-METADATA.json.gitCommit` is semantic provenance, not informational decoration.

## Contract

- `scripts/assert-v26-release-package-identity.ps1` requires `ExpectedSourceCommit` as one exact 40-hex Git SHA.
- The metadata `gitCommit` value must itself be one exact 40-hex SHA and must equal `ExpectedSourceCommit` ordinally after case normalization.
- `.github/workflows/release-v26.yml` passes the runner-provided `$env:GITHUB_SHA` directly to that validator after package construction.
- Existing product/target/framework, release-tag/productVersion, managed assembly version, held-generation, strict UTF-8 and reparse-point protections remain unchanged.
- The qualification-to-hosted-publisher artifact checksum boundary from issue #5209 remains unchanged; this lane adds semantic source identity before that immutable transfer.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-package-source-binding.py
python scripts/preflight-v26-package-release.py
python scripts/preflight-all.py
```

The focused guard requires the workflow SHA argument and metadata comparison and runs negative in-memory mutations that remove the workflow argument, metadata read, SHA-shape admission, or mismatch refusal. Each mutation must be detected.

## Evidence boundary

This is source/release-readiness validation only. Do not dispatch the commercial V26 release workflow, use production signing credentials, or claim licensed BricsCAD `LOCAL_PASS` to qualify this change. Protected Shared CI `preflight + core` is the merge gate.
