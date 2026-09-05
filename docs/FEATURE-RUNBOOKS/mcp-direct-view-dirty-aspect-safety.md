# MCP direct-view dirty/aspect safety

Issue: #5734
Lane-Key: issue-5734
Runtime boundary: source/static/V25 compile is REMOTE_SAFE; licensed BricsCAD direct-view requalification is LOCAL_ONLY.

## Source contract

- `cad_view_zoom_extents` is a transient view operation. It must not call `Database.UpdateExt` or otherwise dirty a clean drawing merely to fit the view.
- `cad_view_set` validates all finite/range inputs before obtaining mutation authority.
- After acquiring the current view, requested width/height must match the current viewport aspect ratio within a narrow deterministic tolerance before any `ViewTableRecord` field is changed. Incompatible aspect requests fail closed rather than allowing BricsCAD to silently normalize one dimension.
- Optional direction input is admissible only when it is effectively the same normalized direction as the current view. A requested direction transition fails closed before changing the view; direct MCP view control must not drive LookFrom/modal direction transitions.
- Existing CMDACTIVE/modal gating remains authoritative immediately before `SetCurrentView`.
- No REGEN or UpdateScreen is forced.

## Deterministic validation

`python scripts/preflight-mcp-direct-view-dirty-aspect-safety.py` must reject the historical `UpdateExt(false)` call and prove aspect/direction compatibility checks execute before the first mutable view assignment.

Protected PR `preflight` and `core`, including trusted V25 compile references and the V25 plugin build, are required on the exact current candidate after latest-main reconciliation.

## Licensed requalification

Hosted CI is not a native verdict. On one exact released descendant containing the fix, a licensed V25 worker should verify:

1. a clean disposable DWG remains clean across `cad_view_zoom_extents` while the view changes as expected;
2. an incompatible width/height aspect request is rejected with no view change and no modal UI;
3. a direction-change request is rejected with no LookFrom warning/dialog and no view change;
4. a compatible center/size request with unchanged direction succeeds;
5. CMDACTIVE/modal refusal and cleanup remain unchanged.

Record exact source/product identity and sanitized result. Do not claim LOCAL_PASS from source/static/compile evidence.