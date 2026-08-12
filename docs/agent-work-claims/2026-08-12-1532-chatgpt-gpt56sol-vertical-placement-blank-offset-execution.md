# Work claim — Vertical placement blank offset execution parity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-vertical-placement-blank-offset-execution-20260812-1532`
- Registered: `2026-08-12T15:32:00+07:00`
- Baseline main SHA: `16f51d92c26b7d0fc067947ea3985c9b8525dc12`
- Priority: P1 fail-closed malformed persisted vertical placement state

## Confirmed defect

`LevelReferenceHealthService` treats an existing blank/whitespace `BottomLevelOffsetM` or `TopLevelOffsetM` as invalid persisted state, but `ElementVerticalPlacementService` currently treats the same value as though the property were absent: `HasConfiguredProperty` returns false for blank values and `OptionalFiniteProperty` returns its fallback. Execution can therefore proceed through a state that Health marks malformed.

## Reserved scope

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs` — offset-presence/blank parsing semantics only.
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementBlankOffsetSmoke.cs` — focused regression; ModuleInitializer may live in this file following existing vertical-placement smoke style.
- this claim file.

## Exclusions

- Do not modify `LevelReferenceHealthService`; its current blank-offset behavior is the parity reference.
- Do not change Floor/Level ID canonicality, floor identity validation, finite arithmetic, hosted-opening containment, category qualification, native placement, Project Browser, README, release/preflight, persistence, or BricsCAD/UI behavior.

## Intended contract

- Missing offset key remains equivalent to offset `0` where currently allowed.
- Existing offset key with null/empty/whitespace payload fails closed rather than silently becoming `0`.
- Existing finite invariant signed offsets remain valid.
- When an offset key exists without its required Level reference, existing `... requires ... LevelId` validation remains authoritative, including for blank payloads.
- Rejection remains read-only with respect to project state.

## Validation plan

Focused Core smoke covers blank bottom offset with a BottomLevelId, blank top offset with both Level refs, blank offset key without its required Level ref, missing-offset controls, and finite signed-offset controls. Source and smoke will be read back from current `main`; ancestry and available GitHub status/workflow evidence will be checked before closeout. No executable/full-build/licensed BricsCAD PASS will be claimed unless actually observed.
