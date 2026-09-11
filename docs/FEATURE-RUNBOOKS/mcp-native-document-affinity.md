# MCP native layer/view active-document affinity

## Scope

This contract applies to the direct MCP layer-state mutations (`cad_layer_set_state`, `cad_layer_restore`) and direct view mutations (`cad_view_zoom_extents`, `cad_view_fit_entities`, `cad_view_set`). These tools capture one BricsCAD `Document` and must keep that exact document authoritative across the native mutation/result boundary.

## Safety contract

- Capture the active `Document` in BricsCAD application context.
- For every mutation, acquire that document's `DocumentLock`, then re-read `Application.DocumentManager.MdiActiveDocument` and require reference identity before opening a write transaction, resolving entity handles for mutation/view work, or obtaining the mutable current view.
- Layer mutations retain one native transaction and validate the complete restore set before write-open. After commit, revalidate exact active-document identity before audit/result publication. A failed post-check is an uncertain result and must not be converted to success or automatically replayed.
- View mutations preserve the existing proven-idle `CMDACTIVE=0`, modal-bit, minimized-window, finite-extents and aspect/direction gates. After `SetCurrentView`, exact active-document identity is revalidated before reading/publishing the resulting view.
- No path retries a native CAD mutation after an affinity failure. The caller must inspect current document/state before deciding whether another explicit request is safe.
- Do not retain `DBObject`, `ObjectId`, `Transaction`, `ViewTableRecord`, or `DocumentLock` beyond their owning native scope.

## Deterministic validation

Run:

```powershell
python scripts/preflight-mcp-native-document-affinity.py
python scripts/preflight-mcp-native-layer-state.py
python scripts/preflight-mcp-direct-view-dirty-aspect-safety.py
python scripts/preflight-mcp-view-extents-modal-safety.py
python scripts/preflight-mcp-production-correctness.py
python scripts/preflight-mcp-capability-lanes.py
```

The dedicated guard requires pre-mutation affinity inside `DocumentLock` and post-mutation affinity before result/audit publication. It intentionally does not treat source/static validation as proof of real MDI timing.

## Runtime classification

Source/preflight and managed compilation evidence are `REMOTE_SAFE`. Real modeless multi-DWG switching, host event timing, disposed native wrappers, and visible view/layer behavior require licensed BricsCAD and remain `LOCAL_ONLY / NO_RESULT` until executed on the exact candidate binary. Hosted CI must never be relabeled as a licensed native PASS.
