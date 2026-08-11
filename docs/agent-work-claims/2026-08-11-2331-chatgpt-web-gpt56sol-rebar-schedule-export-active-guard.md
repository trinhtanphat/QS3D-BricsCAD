# Work claim — Rebar Schedule export active-DWG ownership guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:31:00+07:00`
- Pre-registration observed main SHA: `3688ad298dbaea5db86baf4d941034c8e819815d`
- Registration parent main SHA: `f12cf0d0e50e196c9a24645d29afcb0f270a8a54`
- Claim commit: `4a46ef7b2dacdf8e1f362c2b201514de5de71b67`
- Claim baseline correction: `5394494b2d254da727c816d83439118378ac291f`
- Priority: evidence-driven remote-safe modeless UI hardening

## Coordination note

`main` advanced between the pre-registration HEAD read and the contents-API claim commit. The actual claim parent above is authoritative; the claim-only baseline correction was committed before any substantive source edit.

## Confirmed defect fixed

`RebarScheduleWindow.OnExportClick` previously opened the XLSX `SaveFileDialog` before checking that the modeless BBS window still belonged to the active BricsCAD DWG. The same window already failed closed for Locate, while Door/Opening and Room Finish schedule exports validated ownership before their save dialogs.

BBS export now validates the bound DWG before presenting Save UI and intentionally validates it again after the dialog returns, before resolving current semantic rows and exporting them.

## Implementation surfaces

- `src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs`
- `scripts/preflight-rebar-schedule-export-active-guard.py`
- `docs/UI-REBAR-SCHEDULE-EXPORT-OWNERSHIP-2026-08-11.md`
- this claim file

## Product commits

- `18909aa3ae19aa72359b16b3323fe5b124fe4f1b` — `fix(rebar): guard BBS export before save dialog`
- `96927ba2a4fcec18cfe10ac634f68571b050c652` — `test(rebar): guard BBS export DWG ownership ordering`
- `89e2cce53bc03e2f2241d61167fa995520f441b5` — `docs(rebar): document BBS export ownership guard`

## Acceptance result

1. Export checks `EnsureActive("xuất BBS XLSX")` before constructing/showing `SaveFileDialog`.
2. Export repeats the same check after successful dialog return and before `BuildCurrentRows()`.
3. `BuildCurrentRows()` still uses `TryGetReadOnly`, a detached `ProjectStateSnapshot`, detached preview regeneration, and `ProjectRebarScheduleBuilder.Build(snapshot)`.
4. Locate still checks active-DWG ownership before `ResolveCurrentRow`.
5. The focused preflight encodes these ordering/read-only invariants and no GitHub Actions were dispatched.

## Validation truth

The current `main` source was re-fetched after the writes and contains both ownership gates in the intended order while retaining the detached read-only rebuild path. GitHub compare from implementation commit `18909aa3ae19aa72359b16b3323fe5b124fe4f1b` to then-current `main` reported `behind_by: 0` with the implementation commit as merge base, so concurrent commits had not removed the fix.

The focused preflight file was source-reviewed after push, but no full repository checkout/build or native BricsCAD V25 runtime was available in this remote session; no such PASS is claimed. Existing local modeless/UI qualification remains unchanged.

## Coordination exclusions respected

No Quantity Settings, quantity locate, generated-rebar ownership/touch, core rebar planning, installer, updater, project-session recovery, geometry policy, Ribbon, Workspace, RightPanel, or shared preflight-registration surfaces were edited by this lane.

## Completion condition

Satisfied for remote/source scope: BBS export now fails closed before Save UI when the wrong DWG is active, re-validates after the modal boundary, refreshes only from detached current semantic state, and has a focused regression gate/documentation without overwriting concurrent work.
