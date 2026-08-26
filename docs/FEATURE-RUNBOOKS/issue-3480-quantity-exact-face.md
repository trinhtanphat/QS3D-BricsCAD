# Issue #3480 — Quantity Review exact native BREP face highlight

## Goal

Close the last Model ↔ Quantity gap for Cubicost-like formwork review: clicking an exact face row/value such as `SOLID-01/FACE-03` must highlight only that database-resident native BREP face in BricsCAD, not the whole Solid3d.

## Source contract

1. Quantity Insight continues to obtain exact face identities from the existing `QuantityGeometryExplanationService`; this lane does not introduce a second takeoff/BREP engine.
2. A face action first revalidates active DWG, canonical project/detail row and the current geometry fingerprint through the existing `TryRevalidateQuantityGeometry` path.
3. The displayed face must still exist exactly once with the same face type and gross/deduction/net areas in the freshly rebuilt explanation.
4. `SOLID-xx/FACE-yy` is resolved against the same ordered live source handles, Solid3d filtering and BREP face enumeration used by the explanation service.
5. The database-resident Solid3d is wrapped by a root `FullSubentityPath`, `Brep(rootPath)` is enumerated, and the resolved `Face.SubentityPath` is passed to `Entity.Highlight(path, false)`.
6. Implied whole-entity selection is cleared before the face highlight. Face actions never change entity color/material, open CAD state for write, run Boolean operations or persist presentation state.
7. Previous face highlight is removed when another face/action is chosen, tree/detail selection changes, the panel unloads, or the active document switches.
8. Existing deduction buttons keep their target+cause locate behavior and exact transient intersection/contact preview.

## Source landing / deferred-local handoff

- Feature PR: #3487 — merged.
- Final exact feature head: `977066e1becbf3719a7c6324308288499cc7c57e`.
- Feature merge commit: `9303fb34109e0b5859d8fc2ff1122afdc3cefa83`.
- Exact implementation blob: `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.ExactFace.cs` = `a4e60d3ded1f649bed21ba589c21d855af37ef82`.
- 2026-08-26 audit: current `main@4bf7de082b8a1e6366612b10c69742fd24f5c969` still carries that exact source blob.
- `SOURCE/CI: PASS` for the merged implementation.
- `LOCAL_RUNTIME: PENDING_LOCAL_AGENT`.
- Remote disposition: `DO_NOT_RETRY_REMOTE`.

A later local agent must begin from `docs/LOCAL-AGENT-INBOX.md`, fetch/sync Git, and use a clean exact checkout/worktree before running the matrix below. The intended source-containing checkout recorded by this audit is `4bf7de082b8a1e6366612b10c69742fd24f5c969`. If `main` has advanced, the local agent may validate a newer exact `main` SHA only after confirming the exact-face source blob above is unchanged; otherwise refresh this handoff before runtime qualification. The evidence must always name the exact SHA that actually ran.

## Automated validation

- `python scripts/preflight-quantity-insight-exact-native-face.py`
- aggregate feature source guards
- Core Release build + deterministic smoke
- trusted BricsCAD V25 reference validation
- BricsCAD V25 plugin Release compile
- protected PR `preflight` + `core` on the exact current candidate SHA

Final merged-feature evidence: shared CI run `32557652786` / run number `12461` completed `SUCCESS` on exact head `977066e1becbf3719a7c6324308288499cc7c57e`; protected `preflight` and `core` both passed before PR #3487 merged.

## Licensed V25 interactive acceptance — LOCAL_ONLY

Use a clean checkout of the exact intended SHA and a licensed BricsCAD V25 x64 environment. Do not claim this section PASS from source review, cloud compile, mocks, screenshots without host execution, or `-SkipRuntime`.

Required matrix:

1. Open a QS3D project with a foundation/element whose Quantity Insight explanation exposes at least three exact formwork faces.
2. Open **Quantity Insight → formwork by face** and record the current element/fingerprint without exposing private handles in committed evidence.
3. Click the heading `SOLID-01/FACE-03`; verify only native face #3 highlights and the whole Solid3d is not in implied selection.
4. Click `S gộp` and `S còn` for that same face; verify both route to the same exact face action.
5. Click `SOLID-01/FACE-02`; verify face #3 is unhighlighted before face #2 highlights.
6. Click a volume target/deduction action; verify any face highlight clears and existing target+cause / transient region behavior remains intact.
7. Change tree element/detail selection and confirm the old face highlight clears.
8. Refresh or regenerate so the geometry fingerprint/topology becomes stale; a stale face action must fail closed and leave no stale native highlight.
9. Switch to another DWG and back; no face highlight may leak across documents.
10. Close/hide/unload the panel and verify native highlight cleanup.
11. Save/reopen the DWG and confirm no face color/material/presentation mutation was persisted.

Evidence must include the exact tested QS3D SHA, BricsCAD V25 version, PASS/FAIL for each matrix row, sanitized screenshots/logs if useful, and confirmation that no customer/private DWG, path, ProjectId, raw handle or license data was committed.

## Status boundary

Source/CI success means **implementation + compile qualification only**. Product/runtime completion for `click Face #3 → only native BREP face #3 highlights` remains `PENDING_LOCAL_AGENT` until the licensed matrix above is executed on a real compatible local agent against the exact recorded checkout SHA. Equivalent remote agents must not rerun or simulate this acceptance while it is pending.
