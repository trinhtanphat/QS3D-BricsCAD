# Work claim — Regeneration dirty-subset input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-dirty-subset-input-freshness`
- Registered: `2026-08-12T07:58:00+07:00`
- Baseline main SHA: `70662c5c82b0a119ab0c3f61f4a7439912486ff7`
- Priority: P1 — fail-closed Core regeneration freshness at a remote-safe boundary.

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` materializes caller-provided `IEnumerable<string> elementIds` before it establishes or checks any project freshness invariant. A lazy enumerable can mutate the same `ProjectState` while yielding IDs; the method then continues by reading the dirty set and applying regenerators against a project version that is no longer the version for which input materialization began.

This is a deterministic input-side freshness defect: enumeration is executable caller code, not a passive list read. The failure must be detected before any regeneration mutation is applied.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused auto-discovered preflight under `scripts/` when consistent with current repository convention
- `docs/plans/2026-08-12-regeneration-dirty-subset-input-freshness.md`
- this claim file

## Intended contract

- Capture the project's freshness/version before caller-controlled ID materialization.
- Materialize/normalize IDs exactly once as today.
- If materialization changed the canonical project version, fail closed before reading/applying dirty regeneration work.
- Preserve empty-ID no-op behavior when enumeration is side-effect free.
- Preserve existing dirty subset, missing-element, missing-regenerator, null-builder and generated-artifact semantics.

## Excluded scope

- No persistence, `ProjectStateSnapshot`, XLSX/export, quantity, BricsCAD adapter/UI, installer, release or runtime qualification changes.
- No redesign of regeneration transactions or whole-project regeneration.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Add deterministic Core smoke coverage using a lazy enumerable that mutates `ProjectState` while yielding a dirty element ID.
- Assert fail-closed behavior happens before element definition/channel/timestamp/dirty/generated-artifact mutation.
- Preserve a normal side-effect-free dirty-subset regression path.
- Re-fetch reserved source/test blobs immediately before each write and preserve concurrent `main` history.

## Completion condition

Completed only when the freshness hole is fixed on current `main`, focused regression/preflight coverage is present, exact integration SHAs are recorded, and this claim is marked `COMPLETED` with truthful validation evidence.