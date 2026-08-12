# Work claim — Level Reference blank-offset health integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-level-reference-blank-offset-health`
- Registered: `2026-08-12T11:27:00+07:00`
- Baseline main SHA: `e18adcf4963b5b8d163caa751c54b44f921c9393`
- Priority: P1 — malformed persisted Level offsets must not be health-clean.

## Confirmed defect

`LevelReferenceHealthService` documented `BottomLevelOffsetM` / `TopLevelOffsetM` as finite invariant numbers, but `HasProperty(...)` and `TryOffset(...)` treated a present null/empty/whitespace-only value as if the property were absent. A malformed persisted offset such as `"   "` therefore became numeric zero and could remain health-clean. Canonical Floor mutation writes `"0"` when an offset is introduced and removes the level/offset keys when vertical levels are cleared, so a present blank offset is not a canonical absence representation.

## Reserved scope

- `src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs` — blank persisted offset visibility only
- `tests/QS3D.Core.SmokeTests/LevelReferenceBlankOffsetHealthSmoke.cs` — focused auto-registered regression
- this claim file for close-out

## Implemented contract

- A missing offset property remains the default numeric zero.
- A present null, exact-empty, spaces-only or tab-only offset value is malformed and fails visible.
- With a valid level reference, malformed Bottom/Top offset values produce the existing `BOTTOM_LEVEL_OFFSET_INVALID` / `TOP_LEVEL_OFFSET_INVALID` Error diagnostics.
- Without the corresponding level reference, any present offset property remains structurally configured and produces the existing `*_LEVEL_OFFSET_WITHOUT_LEVEL` Error diagnostic rather than disappearing.
- Valid finite signed offsets, level-reference canonicality, range validation, native-integration pending behavior and Floor mutation behavior remain unchanged.

## Integration evidence

- Claim: `efdb7a2016189856066dc437f29716f1a42e3de3`
- Production fix: `f0217e068600e259a970ebdb65e9993fe879f71d` (`fix(health): surface blank level offsets`)
- Focused regression: `6dfb3f14b540b6f8dc53da2046d1abfdf85f6ed8` (`test(health): guard blank level offsets`)
- Integrated source read-back confirms property presence uses `ContainsKey`, missing keys alone retain the default-zero path, and present null/empty/whitespace offsets fail parsing.
- Integrated smoke read-back covers missing-offset compatibility, null/empty/spaces Bottom offsets, a tab-only Top offset, and a blank offset without its level reference.
- The focused smoke uses `ModuleInitializer`, so no registration-file change was required.

## Excluded scope / validation boundary

- `ProjectFloorService.cs` was not modified because an ACTIVE Floor mutation structural-freshness claim owns that file.
- No vertical-placement execution changes, Model Health UI/preflight script changes, persistence/CAD/runtime work, or unrelated health-provider changes.
- No force-push and no GitHub Actions dispatch.
- No executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed from this remote connector lane; validation here is repository integration/read-back plus focused regression source coverage.