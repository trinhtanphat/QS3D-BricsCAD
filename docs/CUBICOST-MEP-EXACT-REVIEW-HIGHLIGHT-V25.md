# Cubicost-style exact MEP clash transient highlight review — BricsCAD V25

Updated: 2026-08-15 (UTC+7)
Issue: #1649
Upstream exact clash: #1641 / PR #1645

## Scope

This follow-up adds a small review command without duplicating the existing exact-clash detector:

`QS3DMEPEXACTCLASHHIGHLIGHT`

The command is intentionally composable with the existing clash workflow. Use `QS3DMEPEXACTCLASH` to identify exact Solid3d hard clashes and the Locate workflow to isolate a pair, then run the highlight command on exactly two selected entities.

## Acceptance contract

The command:

1. requires exactly two selected snapshots;
2. runs the shared Core recognition profile on both and requires at least one MEP participant;
3. re-resolves both stable Handles immediately before native review;
4. opens both objects `ForRead` and requires two live `Solid3d` objects;
5. re-runs native `Solid3d.CheckInterference` so a stale/non-interfering pair is not highlighted;
6. calls `Highlight()` on both solids inside a short native transaction;
7. changes implied selection only after both highlights succeed;
8. waits for operator Enter/Esc review without keeping the highlight transaction open;
9. in `finally`, re-resolves the pair and calls `Unhighlight()` best-effort;
10. performs outer cleanup only when this command successfully acquired ownership of both highlights.

The ownership rule matters: if exact verification fails before this command highlights the pair, it must not blindly call `Unhighlight()` and potentially clear graphics state created by another tool.

## Failure atomicity

- Unknown/ambiguous classification: no highlight, no selection change.
- Fewer/more than two selected entities: no highlight.
- Stale Handle or non-Solid3d replacement: no highlight.
- `CheckInterference == false`: no highlight.
- First highlight succeeds and second fails: first highlight is removed immediately inside the same transaction while both DBObjects are still valid.
- `SetImpliedSelection`, prompt, cancel or later command exception after both highlights succeed: outer `finally` attempts to remove both owned highlights through fresh Handle resolution.
- If a reviewed object is deleted before cleanup, cleanup remains best-effort and must not mutate another object as a substitute.

## Read-only boundary

The source must not use:

- `OpenMode.ForWrite`;
- `BooleanOperation`;
- clone/copy/append/erase/transform operations;
- project bootstrap or sidecar/QSDB write;
- semantic mutation/audit events;
- `Task.Run`, `Parallel.For`, or native DBObjects across worker threads.

PICKFIRST and transient graphics highlight are interactive editor state, not persisted project/DWG semantic content.

## Why camera zoom is not in this lane

BricsCAD exposes `Editor.GetCurrentView` / `SetCurrentView` and `ViewTableRecord` view properties, but robust zoom-to-WCS-extents requires a deliberately reviewed WCS/DCS transform contract across view direction, twist, model/paper space and viewport state. That will be a separate lane instead of mixing uncertain camera math into a source-safe highlight change.

## LOCAL_ONLY qualification

Run on the exact integrated SHA with licensed BricsCAD V25:

1. Create/choose two recognized Solid3d objects with known native interference. Select exactly the pair and run `QS3DMEPEXACTCLASHHIGHLIGHT`.
2. Confirm both become visibly highlighted, PICKFIRST contains exactly two objects, and both highlights disappear after Enter.
3. Repeat and press Esc; both highlights must disappear.
4. Use a recognized non-interfering pair; command must refuse highlight and preserve existing selection.
5. Use one MEP + one Structure exact clash and an MEP + MEP exact clash.
6. Use a pair with no MEP participant; command must refuse.
7. Exercise unknown/ambiguous recognition and non-Solid3d selection; no highlight.
8. Where a controlled probe can cause second-highlight failure, verify first-highlight cleanup happens immediately with no residual highlight.
9. Verify a failure before highlight ownership does not clear a pre-existing highlight created outside this command.
10. Run across two DWGs and verify Handle/document affinity with no cross-document cleanup.
11. Verify no project, sidecar, audit, geometry, save/reopen or DWG-content mutation occurs.

Status: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`. Remote source/static review is not graphics/runtime PASS.

## Official API basis

Bricsys developer documentation exposes native `Solid3d.CheckInterference`, `Entity.Highlight`, `Entity.Unhighlight`, and `Editor.GetString` APIs. Licensed runtime behavior and graphics cleanup remain subject to the local qualification matrix above.
