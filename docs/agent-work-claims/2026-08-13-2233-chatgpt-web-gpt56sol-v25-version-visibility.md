# Work claim — V25 version visibility + update UX audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T22:33:00+07:00`
- Scope expanded: `2026-08-13T22:35:00+07:00`
- Baseline main SHA: `d0c60af147127aa688679a8374e2fc90a234042f`
- Priority: Owner requested a follow-up audit of the V25 NETLOAD/update work, bug fixes for any remaining source-side gaps, and explicit version visibility in the UI in addition to the version command.

## Reserved scope

Audit the already-implemented V25 GitHub Update Center and version commands, then make the running product version and loaded binary identity clearly visible in the Update Center UI without touching shared Ribbon/Start Center surfaces. Keep `QSVER` / `QS3DVER` as the command-line identity diagnostics, ensure the packaged customer guidance mentions them, and update the repository's authoritative command reference because the new aliases are currently absent there.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs`
- `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs` only if a concrete command bug is found
- `scripts/preflight-v25-netload-update-ux.py`
- `scripts/package-v25.ps1`
- `docs/COMMANDS.md`
- this claim file for closeout

## Excluded scope

- no Ribbon, Start Center, Workspace, palette startup, or NETLOAD lifecycle edits
- no V26 updater changes
- no version bump/tag/release publication
- no GitHub Actions dispatch
- no weakening of package hashes, Authenticode, RemoteSigned, Mark-of-the-Web, or signed-manifest requirements
- no licensed BricsCAD runtime PASS claim from remote work

## Validation plan

- re-read the current updater UI/commands/package scripts before writing
- expose current product version prominently in the Update Center title/header, keep current/latest version state visible, and surface loaded DLL identity for stale-binary diagnosis
- preserve `QSUPDATE`/`QS3DUPDATE` and `QSVER`/`QS3DVER`
- document those commands in `docs/COMMANDS.md`, which is the authoritative command inventory
- extend the focused auto-discovered preflight to guard the UI, command-reference and package-help contracts
- refresh current `main` before each write and preserve concurrent commits
- read back all changed files from `main`; do not dispatch Actions

## Coordination

This lane is limited to the isolated V25 `Updates` UI plus its focused package/preflight/command-reference guidance. It deliberately avoids currently active Core/health/local qualification work and avoids shared Ribbon/Start Center surfaces where other agents may be working.

## Completion condition

The UI/version diagnostics and focused regression/package/command-reference guidance are pushed to current `main`, read back, and this claim is marked `COMPLETED` with exact commit evidence and any remaining LOCAL_ONLY runtime proof stated explicitly.
