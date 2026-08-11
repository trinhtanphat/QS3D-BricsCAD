# Work claim — Semantic tag native-handle property boundary

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-handle-boundary-20260811`
- Registered: `2026-08-11T22:19:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Priority: P2 source-proven documentation safety hardening

## Reserved scope

Close the Core semantic-tag property boundary where `SemanticTagRenderer` rejects canonical/generated ownership slots but still accepts arbitrary ProjectElement property keys containing `Handle` (for example `CadHandle`). Current documentation states native object handles are not semantic annotation values, and the current Interchange ProjectElement portability boundary likewise treats arbitrary handle-bearing element metadata as drawing-local rather than portable semantic data.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticTagRenderer.cs`
- `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs`
- `scripts/preflight-semantic-tags.py`
- this claim file for close-out

## Explicit exclusions

- No changes to native BricsCAD tag create/refresh/remove/runtime-health services or commands.
- No changes to Interchange exporter/importer/property policy.
- No changes to semantic documentation catalog editor/store, DWG Table, Sheet/View/Layout workflows.
- No changes to generated ownership registry or CAD handle ownership semantics.
- No changes to UI/theme, Quantity, reporting, updater/licensing, Direct Draw, Grid, Curtain, rebar, release, or persistence lanes.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Re-fetch latest `main`, active neighboring claims, renderer, smoke and preflight immediately before implementation.
- Preserve all existing supported tag tokens and generated/native owner-slot guards.
- Make `P:` reject arbitrary handle-bearing ProjectElement property names case-insensitively, matching the already-established element-property portability safety boundary without changing Family semantics.
- Add focused smoke coverage proving ordinary semantic properties still render and `CadHandle`/case variants fail closed.
- Extend `preflight-semantic-tags.py` with an exact source/smoke regression guard.
- Validation is Core/source-static in this connector-only environment; no Actions or V25 runtime claim.

## Coordination

The just-completed documentation canonical-ID editor lane is closed and touched a different source surface. This lane is intentionally limited to renderer property exposure plus its existing focused smoke/preflight. It does not overlap the active Core mutation atomicity lane (Navigation/Review/Interchange/Rules and focused persistence), current UI/context-menu, Quantity Settings, Xref scale, locate-preflight, feature-flag, or other recent claims.

## Completion condition

The native-handle property leak is blocked on current `main`, focused regression coverage/preflight are updated without changing native tag lifecycle, current `main` is re-read after integration, and this claim is marked `COMPLETED` with exact implementation commit(s) and validation actually performed.
