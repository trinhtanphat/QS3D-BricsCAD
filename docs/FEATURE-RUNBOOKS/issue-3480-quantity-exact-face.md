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

## Automated validation

- `python scripts/preflight-quantity-insight-exact-native-face.py`
- aggregate feature source guards
- Core Release build + deterministic smoke
- trusted BricsCAD V25 reference validation
- BricsCAD V25 plugin Release compile
- protected PR `preflight` + `core` on the exact current candidate SHA

## Licensed V25 interactive acceptance — LOCAL_ONLY

Use a clean checkout of the exact merged candidate SHA and a licensed BricsCAD V25 x64 environment. Do not claim this section PASS from source review, cloud compile, mocks, screenshots without host execution, or `-SkipRuntime`.

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

Source/CI success means **implementation + compile qualification only**. Product/runtime completion for `click Face #3 → only native BREP face #3 highlights` remains `PENDING_LOCAL` until the licensed matrix above is executed on the exact candidate SHA.
