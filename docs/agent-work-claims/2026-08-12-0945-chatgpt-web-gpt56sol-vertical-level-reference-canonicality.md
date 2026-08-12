# Work claim — Vertical placement Level reference canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:45:00+07:00`
- Baseline main SHA: `5f426b1fc3a5e3b029269ce98ab1cbc814fda418`
- Merge SHA: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Priority: evidence-driven remote-safe execution/health parity

## Reason

`LevelReferenceHealthService` already reports padded or whitespace-only persisted `BottomLevelId` / `TopLevelId` values as canonicality errors, while `ElementVerticalPlacementService` previously trimmed those same stored values before execution. A direct resolver caller could therefore accept `" BOTTOM "` as `BOTTOM`, or treat a whitespace-only Bottom Level reference as absent and fall back to legacy placement, despite the canonical health contract marking that state invalid.

## Completed scope

The vertical-placement execution boundary now rejects persisted Bottom/Top Level references whose raw non-empty spelling differs from its trimmed form. Exact empty/missing references remain the legacy no-Level state; canonical references, signed offsets, existing floor integrity guards and read-only behavior are preserved. Focused module-initializer smoke coverage pins padded Bottom, whitespace-only Bottom, padded Top, canonical references and exact-empty fallback behavior.

## Changed surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementCanonicalLevelReferenceSmoke.cs`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. GitHub `main` readback confirmed the source and smoke after merge. No GitHub Actions were dispatched/rerun and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.