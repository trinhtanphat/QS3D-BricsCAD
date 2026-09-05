# V25 compile-reference extraction cleanup generation safety

Issue: #5631
Lane-Key: issue-5631
Runtime classification: REMOTE_SAFE script/static/CI. No licensed BricsCAD runtime evidence is required for this workflow invariant.

## Defect boundary

`acquire-v25-compile-references.ps1` currently samples existing `ExtractDir` path components for reparse points and later performs `Remove-Item -Recurse` through that pathname. The sample and destructive use are separate path resolutions; therefore the earlier ordinary-directory observation is not a generation proof for the recursive delete.

The pinned MSI already uses a held read generation. Extraction-root cleanup needs an equally fail-closed policy: never recursively reuse an existing pathname merely because it was ordinary at an earlier sample.

## Required contract

- Keep root/cache/MSI overlap and reparse checks.
- Do not recursively delete a pre-existing `ExtractDir` by pathname.
- Require a fresh extraction root. If it already exists, or appears during creation, abort rather than following/reusing it.
- Preserve exact MSI SHA-256, Authenticode, process-tree timeout cleanup, extracted-tree reparse rejection, and V25 assembly validation.
- Do not weaken or skip Shared CI source/build gates.

## Regression

```bash
python scripts/preflight-v25-reference-extract-cleanup-generation.py
```

The deterministic guard is intentionally RED against the current separated check/delete topology. It becomes GREEN only after the production acquisition script removes recursive pathname reuse, rejects existing extraction roots, creates the root without `-Force`, and retains a clear fail-closed diagnostic.

## Validation

Protected Shared CI must pass auto-discovered source guards, tracked PowerShell parsing, deterministic Core smoke, trusted V25 acquisition/reference validation, locked-reference V25 compile and final build on the exact candidate SHA.