# Agent work claim — wall quantity takeoff window

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `COMPLETED`
- Baseline main SHA: `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07`
- Implementation SHA on `main`: `a48f6c30fd062928642c28a538fead8d4073901e`
- Integration: PR `#456` squash-merged to `main`
- Scope: add the owner-requested clean-room BricsCAD-hosted wall quantity/takeoff workspace inspired by the supplied wall screenshot: wall browser, selected-wall facts, detailed per-wall takeoff table, filters/totals, detached recompute and XLSX export.

## Delivered files
- `src/QS3D.BricsCAD.V25/WallQuantityCommands.cs`
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs`
- `scripts/preflight-wall-quantity-window.py`
- `docs/WALL-QUANTITY-TAKEOFF.md`

## Delivered functional contract
- `QS3DWALLQTY` is exposed from a separate command class; the concurrently reserved monolithic `Commands.cs` was not edited;
- only canonical QS3D wall categories are shown: `StructuralWall`, `ArchitecturalWall`, `GlassWall`, `WallPier`;
- left browser supports search + floor/category filtering and drives the right selected-wall facts panel;
- bottom table uses `ProjectQuantityReportBuilder.Detail` from a detached `ProjectStateSnapshot`, never alternate quantity formulas;
- refresh/recompute runs only on the detached snapshot and never saves or mutates the live project;
- XLSX export reuses the existing `XlsxQuantityExporter` and exports the currently visible wall detail rows;
- modeless window is bound to the source BricsCAD `Document`, pins the source `ProjectId`, fails closed on document switch/unavailable/replaced project, and does not bootstrap a project on read-only paths;
- status/totals expose count, length, gross/deduction/net concrete and formwork from the same visible rows;
- thickness/height are display-only explicit instance/Family metadata when present; missing dimensions are not inferred from volume;
- BricsCAD remains the CAD host; this is a WPF/modeless viewer, not a standalone CAD shell.

## Coordination / exclusions preserved
- did not edit `QuantitySummaryWindow.xaml` / `.xaml.cs` owned by the screenshot-1/2 quantity-detail lanes;
- did not edit `src/QS3D.BricsCAD.V25/Commands.cs`;
- did not edit `src/QS3D.Core/Reporting/*`, quantity calculators/formulas, Core persistence/mutation, RightPanel, Ribbon, Start Center or Create Similar;
- no project mutation, save, CAD database writes or hidden project creation from the new viewer;
- no GitHub Actions dispatch/re-run/release;
- no remote claim of native BricsCAD V25 runtime PASS.

## Validation / handoff
- source guard `scripts/preflight-wall-quantity-window.py` checks command/window wiring, wall-category scope, detached regeneration, canonical report/XLSX reuse, key XAML controls/XML parsing and forbidden live-mutation APIs;
- the implementation and documentation were re-fetched from current `main` after merge;
- GitHub combined status for the implementation SHA exposes no status contexts; no workflow was dispatched;
- exact native BricsCAD V25 modeless/filter/recompute/XLSX/DPI qualification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; the executable scenario/evidence contract is recorded in `docs/WALL-QUANTITY-TAKEOFF.md`. The shared `docs/LOCAL-AGENT-INBOX.md` was not rewritten from this remote connector because its available mutation primitive is whole-file replacement and the inbox is concurrently owned/updated by local lanes; destructive replacement risk is explicitly avoided.

## Completion
The dedicated wall takeoff command/window, static source guard and documentation are merged on `main` at `a48f6c30fd062928642c28a538fead8d4073901e`. Native V25 interactive evidence remains a local qualification gate and is not represented as a remote PASS.
