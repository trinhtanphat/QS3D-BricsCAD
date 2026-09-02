# V25 held package metadata provenance

Lane-Key: `issue-5219`

## Problem boundary

The V25 package builder already protects source/package inputs against reparse and generation drift. Post-package provenance checks historically reopened `PACKAGE-METADATA.json` by pathname with ordinary `Get-Content`, so the semantic source/tag decision was not bound to one admitted file generation.

## Contract

- `scripts/assert-v25-release-package-identity.ps1` admits only an ordinary non-reparse metadata file and rejects reparse-backed ancestors.
- The validator opens the metadata with `FileShare.Read`, preventing write/delete/replace while the admitted generation is parsed and checked.
- Metadata is bounded to 64 KiB and decoded with strict UTF-8 before JSON parsing.
- `gitCommit` must be one exact 40-hex commit and equal `ExpectedSourceCommit` after case normalization.
- When `ExpectedReleaseTag` is supplied, `v + productVersion` must equal it ordinally.
- Product/target identity remains `QS3D` / `BricsCAD V25 x64`.
- `scripts/package-v25-release.ps1` reuses the same validator for exact clean-HEAD provenance instead of duplicating a raw pathname JSON read.
- The commercial V25 workflow must invoke the same validator with exact `$env:GITHUB_SHA` and `$env:RELEASE_TAG` immediately before signing rather than re-admitting metadata via `Get-Content`.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-package-metadata-held-provenance.py
python scripts/preflight-all.py
```

The focused guard rejects loss of held read sharing, strict UTF-8, metadata-source comparison, canonical packager reuse, exact workflow source binding, or exact release-tag binding.

## Evidence boundary

This package is REMOTE_SAFE source/release-readiness work. Do not dispatch a production release, import production signing material, or claim licensed BricsCAD runtime evidence to qualify the source change. Protected Shared CI `preflight + core` remains the merge gate.
