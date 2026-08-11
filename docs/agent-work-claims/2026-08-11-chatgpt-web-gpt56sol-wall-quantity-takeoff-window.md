# Agent work claim — wall quantity takeoff window

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Baseline main SHA: `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07`
- Scope: add the owner-requested clean-room BricsCAD-hosted wall quantity/takeoff workspace inspired by the supplied wall screenshot: wall browser, selected-wall facts, detailed per-wall takeoff table, filters/totals, detached recompute and XLSX export.

## Files reserved
- `src/QS3D.BricsCAD.V25/WallQuantityCommands.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml` (new)
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs` (new)
- `scripts/preflight-wall-quantity-window.py` (new)
- `docs/WALL-QUANTITY-TAKEOFF.md` (new)
- this claim file for close-out

## Functional contract
- expose `QS3DWALLQTY` from a separate command class; do not edit the currently reserved monolithic `Commands.cs`;
- show only canonical QS3D wall categories (`StructuralWall`, `ArchitecturalWall`, `GlassWall`, `WallPier`);
- left browser supports search + floor/category filtering and drives a right selected-wall facts panel;
- bottom table uses `ProjectQuantityReportBuilder.Detail` from a detached `ProjectStateSnapshot`, never alternate quantity formulas;
- refresh/recompute runs only on the detached snapshot and never saves or mutates the live project;
- XLSX export reuses the existing `XlsxQuantityExporter` and exports the currently visible wall detail rows;
- modeless window is bound to the source BricsCAD `Document`, fails closed on document switch or unavailable project, and does not bootstrap a project on read-only paths;
- status/totals expose count, length, gross/deduction/net concrete and formwork from the same visible rows;
- preserve clean-room product boundary: BricsCAD remains the CAD host; this is a WPF/modeless viewer, not a standalone CAD shell.

## Explicit exclusions / coordination
- do not edit `QuantitySummaryWindow.xaml` / `.xaml.cs`; active quantity-detail/viewport claims own those screenshot-1/2 surfaces;
- do not edit `src/QS3D.BricsCAD.V25/Commands.cs`; active command-boundary claim owns that file;
- do not edit `src/QS3D.Core/Reporting/*`, wall quantity calculators/formulas, Core persistence/mutation, RightPanel, Ribbon, Start Center or Create Similar;
- no project mutation, save, CAD database writes or hidden project creation from the new viewer;
- no GitHub Actions dispatch/re-run/release;
- no remote claim of native BricsCAD V25 runtime PASS.

## Validation
- add an auto-discovered static preflight for command/window wiring, wall category filter, detached snapshot regen, existing report builder/XLSX reuse, and absence of live project mutation/save calls;
- validate XAML as XML and Python preflight syntax from source-safe tooling where possible;
- re-fetch the final implementation commit and inspect available GitHub status without dispatching workflows.

## Completion condition
The dedicated wall takeoff window and command are committed on current `main`, static source guard is present, documentation records the runtime-local qualification boundary, and this claim is marked `COMPLETED` with the exact implementation SHA.
