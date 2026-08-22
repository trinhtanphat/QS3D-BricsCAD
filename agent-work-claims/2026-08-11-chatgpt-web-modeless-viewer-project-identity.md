# Work claim — modeless viewer project identity hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-modeless-20260811-1931`
- Registered: `2026-08-11T19:31:00+07:00`
- Baseline main SHA: `44dae5d5f3a6184cadf93d27661d1b71dc9bc860`
- Priority: source-proven stale-project boundary in long-lived read-only/modeless viewer callbacks

## Reserved scope

Harden modeless schedule/revision viewer actions so a window opened against one QS3D project cannot locate, export, refresh, or otherwise act against a replacement project that happens to contain semantically identical rows/snapshots. Pin the originating `ProjectId` and fail closed after click-time read-only project re-resolution before semantic freshness checks or callback dispatch.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml.cs` if the same project-identity gap is present
- `src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs` if the same project-identity gap is present
- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs` if the same project-identity gap is present
- direct constructor/callback wiring for those windows only where required to pass the originating project identity
- one existing or focused static preflight covering this modeless project-identity contract
- this claim file for close-out status

## Excluded scope

- No Direct Draw/Create Similar work or any surface reserved by `2026-08-11-chatgpt-web-create-similar.md`.
- No Recognition implementation unless current source regresses; its caller already pins the review `ProjectId` on the audited baseline.
- No Family/Material/Zone/Floor assignment lifecycle changes already covered by earlier hardening.
- No BricsCAD V25 interactive qualification, private DWG work, GitHub Actions dispatch, release, signing, installer, or broad UI redesign.

## Validation plan

- Re-read every affected window and direct caller at the exact implementation baseline.
- Require active drawing identity plus exact originating `ProjectId` before row/snapshot freshness and callback dispatch.
- Extend or add a focused static preflight that checks the originating-project guard and constructor wiring without manufacturing runtime PASS.
- Re-sync `main` before every write, preserve concurrent commits, and inspect the final parent-to-commit diff plus full patched files for truncation.

## Coordination

The active registration-protocol bootstrap is documentation-only. The active Create Similar claim owns Direct Draw authoring and explicitly excludes this modeless viewer lifecycle lane. This claim does not take ownership of those surfaces.

## Completion condition

Confirmed affected modeless viewers are fail-closed across project replacement, focused static regression coverage is pushed, the final diff is verified on current `main`, and this claim is marked `COMPLETED` with actual implementation SHA(s). Any interactive BricsCAD V25 evidence remains explicitly LOCAL_ONLY/unclaimed unless executed by a compatible local agent.
