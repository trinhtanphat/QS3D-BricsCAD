# Work claim — Vertical placement Level reference canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:45:00+07:00`
- Baseline main SHA: `5f426b1fc3a5e3b029269ce98ab1cbc814fda418`
- Priority: evidence-driven remote-safe execution/health parity

## Reason

`LevelReferenceHealthService` already reports padded or whitespace-only persisted `BottomLevelId` / `TopLevelId` values as canonicality errors, but `ElementVerticalPlacementService` still trims those same stored values before execution. A direct resolver caller can therefore accept `" BOTTOM "` as `BOTTOM`, or treat a whitespace-only Bottom Level reference as absent and fall back to legacy placement, despite the canonical health contract marking that state invalid.

## Intended scope

Make the vertical-placement execution boundary reject persisted Bottom/Top Level references whose raw non-empty spelling differs from its trimmed form, while preserving exact empty/missing references as the legacy no-Level state, canonical references, signed offsets, existing floor integrity guards and read-only behavior.

## Changed surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementCanonicalLevelReferenceSmoke.cs`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.