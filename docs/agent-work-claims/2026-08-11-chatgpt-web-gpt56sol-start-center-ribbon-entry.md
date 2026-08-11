# Work claim — Start Center Ribbon entry

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-ribbon-20260811`
- Registered: `2026-08-11T20:20:00+07:00`
- Baseline main SHA: `06c74e79f1c9c8d44672241309d0e80f60ab7733`
- Priority: finish owner-requested Start Center discoverability after the grouped Ribbon information-architecture lane completed.

## Reserved scope

Add exactly one discoverable `QS3DSTART` entry to the current grouped BricsCAD Ribbon information architecture, preferably in `KHỞI ĐẦU` → `Dự án`, without regrouping or removing existing commands. Add a focused static source contract that proves the command appears exactly once in Ribbon and remains bound to the implemented Start Center command.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-start-center-ribbon.py` (new)
- `docs/UI-START-CENTER-2026-08-11.md` only if the discoverability note needs updating
- `docs/LOCAL-AGENT-INBOX.md` only for the exact V25 render/dispatch evidence already required by Start Center
- this claim file for close-out

## Excluded scope

- No regrouping of the 11-tab Ribbon architecture completed by `2026-08-11-chatgpt-web-gpt56sol-ribbon-information-architecture.md`.
- No command implementation changes, Core semantic/persistence changes, Workspace/RightPanel/Theme changes, Direct Draw/Create Similar behavior changes, modeless viewer changes, installer/release/signing or GitHub Actions work.
- No BricsCAD V25 runtime PASS claim from the remote connector lane.

## Validation plan

- Re-fetch current `main`, active claims and the current grouped `RibbonBootstrapper.cs` before editing.
- Preserve all existing Ribbon command strings and panel grouping; add only `Button("Start Center", "QS3DSTART")` in the home/project panel.
- Add an auto-discovered focused preflight that verifies `QS3DSTART` is registered in `StartCenterCommands.cs`, appears exactly once in Ribbon source and remains in the `QS3D_HOME` / `PROJECT` group without fallback to arbitrary command execution.
- Inspect final ancestry/diff; do not dispatch GitHub Actions.

## Completion condition

The single Ribbon entry and focused regression gate are pushed to current `main`, local V25 rendering/dispatch remains explicitly pending, and this claim is marked `COMPLETED` with exact commit SHA(s).