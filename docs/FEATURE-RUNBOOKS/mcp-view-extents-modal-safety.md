# MCP view extents / modal safety qualification

Issue: #5452

## Source contract

- `cad_view_fit_entities` accepts up to 100 distinct live handles.
- A live entity whose `GeometricExtents` throws or is invalid/non-finite is skipped rather than aborting the whole request.
- The result reports fitted `entityCount`, `requestedEntityCount`, `skippedEntityCount`, and `skippedHandles`.
- Missing/erased/non-entity handles remain hard errors; this avoids silently accepting caller identity mistakes.
- If no supplied live entity has usable extents, the request fails closed.
- View mutations check `CMDACTIVE` before view work and again near `SetCurrentView`; bit 8 is reported as modal/dialog state.
- This runtime does not invoke REGEN, REGENALL, or UpdateScreen.

## Hosted CI qualification

Run `scripts/preflight-mcp-view-extents-modal-safety.py` plus protected `preflight` and `core` jobs. Hosted CI establishes source/build regression coverage only.

## LOCAL_ONLY licensed BricsCAD qualification

On the exact build SHA in licensed BricsCAD V25:

1. Open a disposable DWG containing at least one normal entity and one Solid3d known to surface null/uninitialized extents.
2. Call `cad_view_fit_entities` with both handles and `confirmMutation=true`.
3. Confirm the valid entity is fitted and the problematic handle appears in `skippedHandles`; the request must not fail solely because of that handle.
4. Call with only unusable-extents handles and confirm it fails closed with no usable extents.
5. While a real modal/dialog state is present (`CMDACTIVE` bit 8), call a view mutation and confirm it is rejected before changing view state.
6. Confirm no automatic REGEN/REGENALL/UpdateScreen is emitted by the MCP view runtime.

Do not mark LOCAL_ONLY PASS from hosted CI.
