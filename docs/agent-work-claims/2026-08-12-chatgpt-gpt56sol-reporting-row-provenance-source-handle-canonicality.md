# Agent work claim — Reporting row provenance SourceHandles canonicality

Status: ACTIVE
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

The repository's semantic ownership contract already treats `ProjectElement.SourceHandles` as project-owned canonical state: blank/padded entries and duplicate case-insensitive identities fail closed in `SemanticHandleOwnershipResolver`. `ReportingRowProvenance.AppendSourceHandles(...)` currently applies `(raw ?? string.Empty).Trim()`, skips blank entries, and silently ignores duplicate identities. A malformed persisted/in-memory ownership list can therefore be emitted as clean-looking report provenance instead of surfacing the unsafe state.

## Validation boundary

Focused source/readback + Core smoke source only unless an executable build is actually run. No GitHub Actions or BricsCAD runtime PASS is claimed by this lane.
