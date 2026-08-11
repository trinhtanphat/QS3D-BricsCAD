# Work claim — Wall Quantity viewport locate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-viewport-locate`
- Registered: `2026-08-11T21:07:00+07:00`
- Baseline main SHA: `cc38e41349bcb113367670feafbd17238220586c`
- Priority: P1

## Reserved scope

Extend the already-merged `QS3DWALLQTY` modeless wall takeoff so wall list/grid selection can reveal the matching current semantic wall in the active BricsCAD 3D viewport, following the same safe current-row revalidation principles already used by BQ detail review.

## Reserved files

- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs`
- `scripts/preflight-wall-quantity-window.py`
- `docs/WALL-QUANTITY-TAKEOFF.md`
- this claim file for close-out

## Contract

- add a `Bám 3D` opt-in/default-on control plus explicit `Định vị 3D` action;
- selection/double-click must never trust the displayed detached row directly for CAD Handle selection;
- before native selection, revalidate active `Document`, source `ProjectId`, current semantic ElementId, wall category and current detached detail row;
- resolve source Handles again from the current canonical project, then use the existing `CadHandleService.Select(...)` + `QS3DZOOMSELECTED` path;
- missing/deleted/retyped/stale semantic rows must fail closed and preserve project/CAD semantics;
- no quantity-formula, Core Reporting, persistence, Ribbon, `Commands.cs`, RightPanel or other concurrently owned surface edits;
- no GitHub Actions dispatch and no remote claim of licensed V25 runtime PASS.

## Validation

Strengthen `scripts/preflight-wall-quantity-window.py` to guard current-row revalidation order, Handle re-resolution, native selection/zoom wiring, auto-reveal control and absence of project/native mutation APIs.

## Completion condition

Current `main` contains the wall takeoff 3D reveal behavior, strengthened static guard and documentation, with this claim marked `COMPLETED` and exact implementation SHA recorded.
