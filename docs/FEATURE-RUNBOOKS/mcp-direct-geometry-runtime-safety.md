# MCP direct geometry/runtime safety

Issue: #5449

This lane hardens the non-overlapping portion of the reported MCP/BricsCAD runtime defects. The active save/eCantOpenFile carrier remains #5441 / PR #5442 and is intentionally not modified here.

## Source contracts

### Direct extrusion

`cad_extrude` must convert the selected closed planar `Curve` to exactly one transient `Region` using `Region.CreateFromCurves`, then pass that Region to `Solid3d.CreateExtrudedSolid`. The transient Region is disposed after use. Native BricsCAD `EXTRUDE` remains an independent bounded command fallback.

### Boolean operations

`cad_boolean_union`, `cad_boolean_subtract`, and `cad_boolean_intersect` must clone the database-resident tool `Solid3d`, use the transient clone as the Boolean kernel operand, and erase the original tool entity only after the kernel call succeeds. The transient clone is disposed in all cases.

### Legacy Solid3d extents

Detailed `Solid3d` inspection does not eagerly call `GeometricExtents`; it reports `extents:null` with `extentsDeferred:true`. Generic extents reads are exception-bounded and return `null` instead of letting legacy `eNullExtents` escape through MCP.

### REGENALL / modal command safety

MCP native command dispatch must never queue `REGENALL`. It is rejected before `MutationGate` acquisition because BricsCAD can enter modal `CMDACTIVE` bit 8 during this command and strand logical writer ownership. In addition, the coordinator reads `CMDACTIVE` while arming any native command and fails closed when bit 8 is already set. Finish/cancel the modal host command before retrying another native MCP command.

Use bounded view/status APIs for remote view refresh workflows rather than forcing `REGENALL` through `cad_command_sequence`.

## Source validation

Run:

```powershell
python scripts/preflight-mcp-direct-geometry-runtime-safety.py
```

This source guard proves the static contracts above. Licensed BricsCAD runtime behavior remains separately qualified; static CI must not be described as a live-host PASS.

## Runtime qualification

On the exact built V25 plugin in licensed BricsCAD:

1. create a closed planar curve and call `cad_extrude`; confirm a valid `Solid3d` result;
2. create overlapping solids and exercise subtract/union/intersect; confirm no database-resident operand `eInvalidInput` regression;
3. inspect legacy `Solid3d` entities known to have unreliable extents and confirm MCP returns bounded extents metadata rather than an uncaught `eNullExtents`;
4. call `cad_command_sequence` with `REGENALL` and confirm it fails before native queueing/writer ownership;
5. while BricsCAD is in a modal state where `CMDACTIVE & 8 != 0`, attempt another native MCP command and confirm retryable fail-closed behavior;
6. separately qualify `cad_save`/QSAVE under #5441/#5442; do not use this lane as save evidence.
