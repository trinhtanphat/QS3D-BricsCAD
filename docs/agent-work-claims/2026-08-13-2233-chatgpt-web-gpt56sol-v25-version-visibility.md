# Work claim — V25 version visibility + update UX audit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T22:33:00+07:00`
- Scope expanded: `2026-08-13T22:35:00+07:00`
- Completed: `2026-08-13T22:50:00+07:00`
- Baseline main SHA: `d0c60af147127aa688679a8374e2fc90a234042f`

## Completed scope

Audited the V25 update/version path. The version commands were already present, so no duplicate command implementation was added. Added prominent version and loaded-DLL identity to the Update Center UI, strengthened the focused source guard, and added a dedicated command reference.

## Outcome

- `QS3DUPDATE` and `QSUPDATE` open the Update Center.
- `QS3DVER` and `QSVER` report product version, assembly version, loaded DLL path and update state.
- The Update Center now shows the current product version in its title/header, current/latest version comparison and the exact loaded DLL path.
- Current V25 product version remains `0.1.0-preview.5`; this lane did not bump the release version.

## Changed surfaces

- `src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs`
- `scripts/preflight-v25-netload-update-ux.py`
- `docs/V25-UPDATE-VERSION-COMMANDS.md`
- this claim file

`UpdateCommands.cs`, `scripts/package-v25.ps1` and `docs/COMMANDS.md` were audited read-only. The existing command implementation was already correct, package command discovery already reads `[CommandMethod]` declarations into `COMMANDS.txt`, and the dedicated update/version reference records the user-facing command set without rewriting the broad shared command inventory during concurrent work.

## Completion evidence

- `cd90180e8859c880f77e84f5fdb78e41c5025853` — claim registration
- `c95538130fbbc3052e2f55a9fda6940b873c2038` — scope expansion
- `3071f660f2f0b2684085ca726d3d8674586803f2` — visible version and loaded-DLL UI
- `d62f191130ef0d1091689c9b3bed922bd6959a34` — focused source guard
- `5fed82ebe0aa9db2a8e6f4d5d89a43c87efd4cb2` — update/version command reference

## Validation

Re-fetched the current command source, Update Center source, focused guard and V25 project metadata from `main`. Concurrent commits were preserved. No GitHub Actions run or release publication was started in this lane. Exact BricsCAD V25 runtime behavior remains a separate local qualification boundary.
