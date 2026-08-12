# Work claim — Regeneration dirty-subset input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-dirty-subset-input-freshness`
- Registered: `2026-08-12T07:58:00+07:00`
- Completed: `2026-08-12`
- Baseline main SHA: `70662c5c82b0a119ab0c3f61f4a7439912486ff7`
- Priority: P1 — fail-closed Core regeneration freshness at a remote-safe boundary.

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` materialized caller-provided `IEnumerable<string> elementIds` before it established or checked any project freshness invariant. A lazy enumerable could mutate the same `ProjectState` while yielding IDs; the method then continued by reading the dirty set and applying regenerators against a project version that was no longer the version for which input materialization began.

This was a deterministic input-side freshness defect: enumeration is executable caller code, not a passive list read. The failure now gets detected before any regeneration mutation is applied, including the side-effecting empty-enumerable case.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused auto-discovered preflight under `scripts/`
- `docs/plans/2026-08-12-regeneration-dirty-subset-input-freshness.md`
- this claim file

## Implemented contract

- Capture the project's `ChangeVersion` immediately before caller-controlled ID materialization.
- Materialize/normalize IDs exactly once as before.
- If materialization changed the canonical project version, fail closed with `InvalidOperationException` immediately after materialization.
- Perform that freshness rejection before the zero-target no-op and before regeneration-side mutation.
- Preserve empty-ID no-op behavior when enumeration is side-effect free.
- Preserve existing dirty subset, missing-element, missing-regenerator, null-builder and generated-artifact semantics.

## Evidence

- Plan: `69a7b4006ddafb4cc8bfbb09a9cd3b18a618d87d`
- Source fix: `7e2414f06daa06036368b27278534d0ac207ed3b`
- Deterministic smoke regression: `a8eb48d5bf13ddc6f6badf8808de64b975c2ffd3`
- Smoke registration: `2406da81d6177e674853cfa9f860ef3540ca931e`
- Static preflight: `7232f656e107b5aaa57a50da42254d6acdbd9588`

## Validation evidence

- Source, deterministic smoke coverage, smoke registration and focused static preflight are committed on `main`.
- The preflight locks ordering as: version capture → `CanonicalTargetIds(...)` materialization → freshness comparison → zero-target no-op.
- Smoke coverage includes stable lazy input, mutating lazy input with a target, and mutating lazy input that yields no targets.
- This remote connector session did not execute the full Core smoke executable, GitHub Actions or BricsCAD V25 runtime; no PASS claim is made for those environments.

## Excluded scope

- No persistence, `ProjectStateSnapshot`, XLSX/export, quantity, BricsCAD adapter/UI, installer, release or runtime qualification changes.
- No redesign of regeneration transactions or whole-project regeneration.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion condition

`COMPLETED`: the freshness hole is fixed on `main`, focused regression/preflight coverage is present, exact integration SHAs are recorded, and validation limitations are stated explicitly.
