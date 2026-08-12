# Work claim — Level Reference blank-offset health integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-level-reference-blank-offset-health`
- Registered: `2026-08-12T11:27:00+07:00`
- Baseline main SHA: `e18adcf4963b5b8d163caa751c54b44f921c9393`
- Priority: P1 — malformed persisted Level offsets must not be health-clean.

## Confirmed defect

`LevelReferenceHealthService` documents `BottomLevelOffsetM` / `TopLevelOffsetM` as finite invariant numbers, but `HasProperty(...)` and `TryOffset(...)` currently treat a present null/empty/whitespace-only value as if the property were absent. A malformed persisted offset such as `"   "` therefore becomes numeric zero and can remain health-clean. Canonical Floor mutation writes `"0"` when an offset is introduced and removes the level/offset keys when vertical levels are cleared, so a present blank offset is not a canonical absence representation.

## Reserved scope

- `src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs` — blank persisted offset visibility only
- `tests/QS3D.Core.SmokeTests/LevelReferenceBlankOffsetHealthSmoke.cs` — new focused auto-registered regression
- this claim file for close-out

## Intended contract

- A missing offset property remains the default numeric zero.
- A present null, exact-empty, spaces-only or tab-only offset value is malformed and must fail visible.
- With a valid level reference, malformed Bottom/Top offset values produce the existing `*_LEVEL_OFFSET_INVALID` Error diagnostics.
- Without the corresponding level reference, any present offset property remains structurally configured and produces the existing `*_LEVEL_OFFSET_WITHOUT_LEVEL` Error diagnostic rather than disappearing.
- Preserve valid finite signed offsets, level-reference canonicality, range validation, native-integration pending behavior and all Floor mutation behavior.

## Excluded scope

- Do not modify `ProjectFloorService.cs`; an ACTIVE Floor mutation structural-freshness claim owns that file.
- No vertical-placement execution changes in this lane, no Model Health UI/preflight script changes, no persistence/CAD/runtime work.
- No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or BricsCAD V25/V26 runtime qualification claim.

## Validation plan

Refresh source after claim registration, change only property-presence/offset parsing semantics, add focused regression for valid/missing plus empty/space/tab cases, read back integrated source/test, close the claim with exact SHAs and verify completion ancestry on `main`.