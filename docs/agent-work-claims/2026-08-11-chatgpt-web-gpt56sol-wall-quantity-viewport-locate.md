# Work claim — Wall Quantity viewport locate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-viewport-locate`
- Registered: `2026-08-11T21:07:00+07:00`
- Completed: `2026-08-11T21:22:00+07:00`
- Baseline main SHA: `cc38e41349bcb113367670feafbd17238220586c`
- Implementation SHA on `main`: `f66037846067cae4d2bba429e721a8ff4f228c71`
- Integration: PR `#461` squash-merged to `main`
- Priority: P1

## Delivered scope

Extended the already-merged `QS3DWALLQTY` modeless wall takeoff so wall list/grid selection can reveal the matching current semantic wall in the active BricsCAD 3D viewport, following the same safe current-row revalidation principles used by BQ detail review.

## Delivered files

- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs`
- `scripts/preflight-wall-quantity-window.py`
- `docs/WALL-QUANTITY-TAKEOFF.md`

## Delivered contract

- added default-on `Bám 3D` plus explicit `Định vị 3D`;
- wall-list and detail-grid selection reveal the current wall when `Bám 3D` is enabled;
- when automatic reveal is disabled, list/grid double-click uses the same guarded locate path;
- displayed detached rows are never trusted directly for CAD Handle selection;
- before native selection, locate revalidates active `Document`, pinned source `ProjectId`, current semantic ElementId, current wall category and an exactly rebuilt detached current detail row;
- source Handles are then re-resolved from the current canonical project before `CadHandleService.Select(...)` and `QS3DZOOMSELECTED`;
- missing/deleted/retyped/stale/project-replaced/no-live-Handle cases fail closed before selecting or zooming the wrong object;
- no quantity formula, Core Reporting implementation, persistence, Ribbon, `Commands.cs`, RightPanel or native geometry builder was changed;
- viewport reveal changes only current CAD selection/view and does not mutate semantic state or save `.qsdb`.

## Validation / coordination

- `scripts/preflight-wall-quantity-window.py` was strengthened to guard command/window wiring, detached regeneration, canonical report/export reuse, auto/explicit reveal controls, current-row validation tokens, current Handle re-resolution, and the `EnsureCurrentProject -> ResolveCurrentRow -> SourceHandleResolver -> CadHandleService.Select -> QS3DZOOMSELECTED` ordering;
- PR `#461` changed exactly the four reserved product/test/doc files;
- branch-head workflow lookup exposed no GitHub Actions workflow runs and this lane did not dispatch/re-run a workflow;
- current source was reviewed through the PR patch before squash merge;
- no force push or history rewrite was used.

## LOCAL_ONLY disposition

Licensed BricsCAD V25 modeless mouse interaction, implied-selection highlight, actual viewport zoom, stale Handle behavior, multi-DWG switching, HiDPI layout and real XLSX dialog/export remain `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`. These scenarios belong in the repository's single `docs/LOCAL-AGENT-INBOX.md` queue and must not be represented as a remote runtime PASS.

## Completion

The guarded Wall Quantity 3D reveal path, stronger static contract and documentation are merged on `main` at `f66037846067cae4d2bba429e721a8ff4f228c71`. The source lane is complete; licensed interactive qualification remains local-only.
