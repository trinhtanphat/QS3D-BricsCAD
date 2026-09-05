# V25 held commercial release upload

## Scope

This runbook covers the source-safe invariant for uploading the four V25 commercial draft assets after the build/sign job boundary. It does not dispatch a production release, use signing credentials, or claim licensed BricsCAD runtime evidence.

## Invariant

The release job must bind each local candidate asset to one admitted Windows file generation before any `gh release upload` call. Every admitted stream is opened read-only with `FileShare.Read`, which permits the GitHub CLI to read the file while denying write/delete sharing that could replace the pathname generation during upload.

For every asset, the uploader records the exact admitted name, length, and SHA-256 from the held stream. All asset streams stay open through every draft upload. The workflow then compares GitHub's reported asset size and the downloaded draft SHA-256 against those admitted values; it must not reopen the original candidate pathname to reconstruct local evidence.

The helper rejects directories, reparse points, empty/duplicate asset names, upload failure, pathname type/length continuity loss, and disposes every held stream fail-closed.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-held-release-upload.py
```

The auto-discovered preflight checks both the helper and `.github/workflows/release-v25.yml`. Mutation cases ensure the guard rejects write sharing, removed reparse rejection, missing held hashing, moving upload outside the held helper, and workflow regressions that reopen local paths for length/hash evidence.

## Acceptance boundary

Repository acceptance is protected PR `preflight + core` on the exact candidate plus latest-main collision/freshness reconciliation. No commercial workflow dispatch is required or permitted for remote validation of this source change. Any licensed V25 execution remains separately classified by the repository's LOCAL_ONLY rules.
