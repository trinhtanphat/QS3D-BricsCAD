# Work claim — Semantic tag native-handle property boundary

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-handle-boundary-20260811`
- Registered: `2026-08-11T22:19:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Claim commit: `31a79aa3c196e7082c15f0340e6924f851703b78`
- Source fix commit: `38961329b554cefc6c390621dcacab1acf4d24f4`
- Regression smoke commit: `059c488d8eaf473bab5e6d30444640a84b5775b9`
- Focused preflight commit: `ca469570830d627bb84c7abf18520096aaa027f6`
- Priority: P2 source-proven documentation safety hardening

## Reserved scope

Close the Core semantic-tag property boundary where `SemanticTagRenderer` rejects canonical/generated ownership slots but still accepts arbitrary ProjectElement property keys containing `Handle` (for example `CadHandle`). Current documentation states native object handles are not semantic annotation values, and the current Interchange ProjectElement portability boundary likewise treats arbitrary handle-bearing element metadata as drawing-local rather than portable semantic data.

## Implemented surfaces

- `src/QS3D.Core/Documentation/SemanticTagRenderer.cs`
- `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs`
- `scripts/preflight-semantic-tag-handle-boundary.py`
- this claim file for close-out

The original validation plan named the large existing `scripts/preflight-semantic-tags.py`. To avoid replacing that broad, concurrently sensitive native lifecycle gate through a connector-only full-file write, implementation instead added a narrow `preflight-semantic-tag-handle-boundary.py`. `scripts/preflight-all.py` auto-discovers every `preflight-*.py`, so the focused guard is included without modifying unrelated native tag checks.

## Implemented fix

- Preserve all existing canonical/generated owner-slot exclusions.
- Reject any ProjectElement `P:` property key containing `Handle` with `StringComparison.OrdinalIgnoreCase` before rendering.
- Keep ordinary semantic properties and quantity tokens unchanged.
- Add smoke coverage for `CadHandle` and `SourceHandleRef`, including mixed/upper-case template selectors, while retaining the ordinary `Mark` property rendering regression.
- Add an auto-discovered focused static gate covering both the renderer guard and smoke regression tokens.

## Explicit exclusions honored

- No native BricsCAD tag create/refresh/remove/runtime-health services or commands changed.
- No Interchange exporter/importer/property policy changed.
- No semantic documentation catalog editor/store, DWG Table, Sheet/View/Layout workflow changed.
- No generated ownership registry or CAD handle ownership semantics changed.
- No UI/theme, Quantity, reporting, updater/licensing, Direct Draw, Grid, Curtain, rebar, release, or persistence lane changed.
- No GitHub Actions dispatched and no BricsCAD V25 runtime PASS claimed.

## Validation actually performed

- Re-fetched `SemanticTagRenderer.cs` from current `main` after integration and verified the case-insensitive `Handle` exclusion is present alongside all prior generated/native guards.
- Re-fetched `SemanticTagRendererSmoke.cs` from current `main` and verified `NativeHandleMetadataCannotLeakIntoTag` is registered in `Run()`, covers `CadHandle` / `SourceHandleRef`, and the ordinary property/quantity regression remains intact.
- Re-fetched `preflight-semantic-tag-handle-boundary.py` from current `main` and verified its exact source/smoke guard tokens.
- Re-fetched `scripts/preflight-all.py`; its `preflight-*.py` glob auto-discovers the focused gate.
- Compiled the focused Python preflight source with `python3 -m py_compile` successfully in the available local container.
- The available container has no `dotnet`, `csc`, or `mcs`, so no local Core build/smoke execution was claimed. No GitHub Actions were dispatched. No BricsCAD V25 runtime qualification was claimed.
- Re-read recent `main` commits after the writes; concurrent changes remained outside this reserved renderer/smoke/preflight scope and no force push was used.

## Coordination

The just-completed documentation canonical-ID editor lane was closed and touched a different source surface. This lane remained limited to renderer property exposure plus focused smoke/preflight. It did not overlap the active Core mutation atomicity lane (Navigation/Review/Interchange/Rules and focused persistence), UI/context-menu, Quantity Settings, Xref scale, locate-preflight, feature-flag, title-block parameter map, rebar, recognition, or other recent claims.

## Completion condition

Completed. The native-handle property leak is blocked on current `main`, focused regression coverage and an auto-discovered preflight are committed, current `main` was re-read after integration, and this claim records exact commits plus validation actually performed.
