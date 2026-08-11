# Work claim — Rebar Schedule export active-DWG ownership guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:31:00+07:00`
- Baseline main SHA: `3688ad298dbaea5db86baf4d941034c8e819815d`
- Priority: evidence-driven remote-safe modeless UI hardening

## Confirmed defect

`RebarScheduleWindow.OnExportClick` opens the XLSX `SaveFileDialog` before checking that the modeless BBS window still belongs to the active BricsCAD DWG. The same window already fail-closes Locate on DWG ownership, and Door/Opening plus Room Finish schedule exports validate ownership before their save dialogs. With a different DWG active, BBS can therefore ask the user to choose/confirm a destination file and only reject the operation afterward.

This is a sequencing/ownership defect, not a request to weaken document-bound modeless behavior.

## Planned implementation surfaces

- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs`
- `scripts/preflight-rebar-schedule-export-active-guard.py`
- `docs/UI-REBAR-SCHEDULE-EXPORT-OWNERSHIP-2026-08-11.md`
- this claim file

## Acceptance

1. Export validates the bound DWG before opening the save dialog.
2. Export re-validates ownership after the dialog returns, before rebuilding/exporting current rows, so a delayed/modal boundary cannot target another DWG.
3. Existing read-only detached-snapshot refresh, XLSX exporter, Locate freshness resolution and `DocumentBoundWindowLifetime` behavior remain unchanged.
4. Focused source preflight guards ordering without dispatching GitHub Actions.
5. No native BricsCAD V25 runtime PASS is claimed from the remote environment.

## Coordination exclusions

No Quantity Settings, quantity locate, generated-rebar ownership/touch, core rebar planning, installer, updater, project-session recovery, geometry policy, Ribbon, Workspace, RightPanel, or shared preflight-registration surfaces are reserved by this claim.