# Work claim — Vertical placement blank offset execution parity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-vertical-placement-blank-offset-execution-20260812-1532`
- Registered: `2026-08-12T15:32:00+07:00`
- Completed: `2026-08-12T15:36:31+07:00`
- Baseline main SHA: `16f51d92c26b7d0fc067947ea3985c9b8525dc12`
- Claim commit: `f321acd7a56699f7d3104c52b3c4e4a175776fcb`
- Source fix: `284e5bdf1b91d56c5a9b1658073611e3594c973e`
- Regression smoke: `5ce4100a86cbb1935f7c1468c7decb7027dc01f1`
- Priority: P1 fail-closed malformed persisted vertical placement state

## Confirmed defect

`LevelReferenceHealthService` treated an existing blank/whitespace `BottomLevelOffsetM` or `TopLevelOffsetM` as invalid persisted state, but `ElementVerticalPlacementService` treated the same value as though the property were absent: `HasConfiguredProperty` returned false for blank values and `OptionalFiniteProperty` returned its fallback. Execution could therefore proceed through a state that Health marked malformed.

## Completed scope

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs` now distinguishes property presence from a missing property and only applies the optional numeric fallback when the key does not exist.
- Existing present blank/null/whitespace offset payloads now fail through the existing finite-invariant-number diagnostic.
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementBlankOffsetSmoke.cs` uses `ModuleInitializer` and covers blank bottom/top offsets, blank orphan offset presence, missing-offset fallback, finite signed offsets, direct `ReadLevelOffset`, and read-only rejection.
- this claim file.

## Exclusions preserved

- `LevelReferenceHealthService` unchanged and remains the parity reference.
- No Floor/Level ID canonicality, floor identity validation, finite arithmetic, hosted-opening containment, category qualification, native placement, Project Browser, README, release/preflight, persistence, or BricsCAD/UI behavior changes.

## Result

- Missing offset key remains equivalent to offset `0` where currently allowed.
- Existing offset key with blank/whitespace payload fails closed instead of silently becoming `0`.
- Existing finite invariant signed offsets remain valid.
- Existing offset-without-Level validation remains authoritative even for blank payloads.
- Rejection is read-only with respect to project state.

## Validation evidence

- Source commit diff changes only `HasConfiguredProperty` and `OptionalFiniteProperty` in `ElementVerticalPlacementService.cs`.
- Pinned readback at main SHA `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1` confirmed source blob `069fb2ac6181e849c4b4519956f4b4fc34fbb976` and smoke blob `31586624aaa2450f78743f027a57cc1f46ce3221`.
- Ancestry compare from claim `f321acd7a56699f7d3104c52b3c4e4a175776fcb` to pinned main `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1` is `ahead` with no divergence; concurrent release/preflight commits are preserved.
- GitHub reported no combined status checks and no workflow runs for regression commit `5ce4100a86cbb1935f7c1468c7decb7027dc01f1`.
- The available local runtime has no `dotnet` executable, so no executable smoke/full-build/licensed BricsCAD PASS is claimed for this lane.
