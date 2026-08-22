# Agent work claim — Reporting row provenance SourceHandles canonicality

Status: COMPLETED
Owner: ChatGPT Web / GPT-5.6 Sol
Registered: 2026-08-12
Baseline main: 178af224adb9bb0c3009a9a67f7d5eacd290019a

## Scope

- `src/QS3D.Core/Reporting/ReportingRowProvenance.cs`
  - Stop silently trimming/skipping/deduplicating malformed project-owned stored SourceHandles while building report provenance.
  - Require each stored handle to be nonblank and already trimmed/canonical, and reject duplicate handle identity instead of hiding it.
- `tests/QS3D.Core.SmokeTests/ReportingRowProvenanceCanonicalSourceHandleSmoke.cs`
  - Regression for padded, blank, duplicate stored SourceHandles plus unchanged canonical report provenance.
- This claim file only.

## Defect evidence

The repository's semantic ownership contract already treats `ProjectElement.SourceHandles` as project-owned canonical state: blank/padded entries and duplicate case-insensitive identities fail closed in `SemanticHandleOwnershipResolver`. `ReportingRowProvenance.AppendSourceHandles(...)` previously applied `(raw ?? string.Empty).Trim()`, skipped blank entries, and silently ignored duplicate identities. A malformed persisted/in-memory ownership list could therefore be emitted as clean-looking report provenance instead of surfacing the unsafe state.

## Delivery

- Claim: `9aae627ac0f6e167760c6d8482c84b87d2c5014b`
- Source fix: `595309c39e1d1a7cd47c8bb6043ca2245d24bbf2`
- Regression smoke: `e06120487fa00a242f6571f25c8671f8ec90b538`

The reporting provenance helper now rejects blank and non-trimmed project-owned SourceHandles and rejects duplicate case-insensitive handle identity instead of normalizing or deduplicating it. Canonical unique handles are preserved unchanged. Focused smoke coverage exercises the public Door/Opening schedule path for all rejection cases and the stable canonical path.

## Validation boundary

Source and smoke files were read back from `main`. No GitHub Actions, executable .NET/Core smoke run, or BricsCAD runtime PASS is claimed by this lane.
