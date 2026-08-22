# Work claim — Vertical placement global Floor integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-vertical-placement-floor-integrity-20260812-0929`
- Registered: `2026-08-12T09:29:00+07:00`
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

## Validation plan

- Seed valid Bottom/Top Floors plus an unrelated duplicate pair and prove Floor-based bottom/top/effective-height calculations fail closed.
- Seed valid Bottom/Top Floors plus an unrelated null Floor entry and prove the same fail-closed behavior.
- Preserve valid canonical Floor-based placement and direct HeightM fallback behavior.
- Keep the service read-only: no `ChangeVersion`, element/property or Floor mutation.
- Read back source/test and verify ancestry on moving `main`; no GitHub Actions or licensed BricsCAD runtime PASS claim.

## Coordination

Historical vertical-placement lanes covered project-element validation, exact element ownership, duplicate semantic element IDs and floor/level relation freshness. This reservation is limited to global Floor collection integrity during Floor-based vertical placement and does not reopen those completed contracts.

## Completion condition

Floor-based vertical placement rejects globally malformed Floor collections before level resolution, focused deterministic Core smoke coverage is pushed, and this claim is marked `COMPLETED` without dispatching Actions.
