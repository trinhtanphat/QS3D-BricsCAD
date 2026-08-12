# Work claim — Vertical placement global Floor integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-vertical-placement-floor-integrity-20260812-0929`
- Registered: `2026-08-12T09:29:00+07:00`
- Completed: `2026-08-12T09:54:00+07:00`
- Baseline main SHA: `89df6e78a93ba37ac320610ed53082782c60fad2`
- Priority: P1 — Floor-based vertical placement must not compute from a project whose Floor identity collection is globally malformed.

## Reserved scope

Harden `ElementVerticalPlacementService` so Floor-based placement (`BottomLevelId`/`TopLevelId`) fails closed before resolving levels when `project.Floors` contains a null entry or case-insensitive duplicate Floor IDs, including duplicates unrelated to the referenced Bottom/Top Floors. Direct `HeightM` fallback without level metadata remains outside the new Floor-collection preflight.

## Expected surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- `ProjectFloorService` target/Create integrity, already completed.
- Floor/Zone UI, reporting, persistence/interchange, model health, native BricsCAD adapters, Actions/release.
- Existing element ownership/duplicate semantic element/freshness and numeric offset guards.
- No change to direct HeightM fallback behavior when Bottom/Top Level metadata is absent.

## Implementation

- Source fix: `d28463f41bafa87d640caef218190a6d605484b8` (`fix(vertical): reject malformed floor identity state`).
- Regression: `c2e3eff6e352d460760898127233272056cc7ec3` (`test(vertical): guard global floor identity integrity`).

## Validation

- Floor-based `Resolve` / `ResolveEffectiveHeight` now reject unrelated case-insensitive duplicate Floor IDs before target level resolution.
- The same paths reject a null entry anywhere in `project.Floors`.
- Valid Bottom/Top placement remains unchanged.
- Direct legacy height fallback without level metadata remains unchanged and does not run the new Floor collection preflight.
- Regression snapshots `ChangeVersion`, `UpdatedUtc`, Floor count and element property count to prove malformed-state rejection remains read-only.
- Source/test commits were read back from GitHub and remain reachable on moving `main`.
- No GitHub Actions dispatched; no local compile or licensed BricsCAD runtime PASS claimed.

## Completion condition

Satisfied. Floor-based vertical placement rejects globally malformed Floor collections before level resolution, focused deterministic Core smoke coverage is pushed, and ownership is released as `COMPLETED`.
