# Work claim — V25 NETLOAD bootstrap + update command UX

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T22:18:00+07:00`
- Completed: `2026-08-13T22:30:00+07:00`
- Baseline main SHA: `c33f8404cb0fc66641741970b23a9cd5b6d5e03d`
- Priority: Owner-reported V25 `NETLOAD` failure with HRESULT `0x80131515` / downloaded-package dependency load failure, plus request for direct GitHub update commands.

## Reserved scope

Harden the customer-facing V25 NETLOAD/update experience around the existing secure package/install/update chain, expose short GitHub update/version command aliases, and make stale/wrong loaded-binary identity visible without weakening BricsCAD or Windows security.

## Implemented outcome

Repository readback established that the V25 package already contains the correct integrity-checked Mark-of-the-Web remediation path: `INSTALL-QS3D.cmd` uses `RemoteSigned`, validates the installer signature state, and invokes `install-v25-autoload.ps1`; that installer validates the package SHA-256/identity before copying and `Unblock-File`-ing the installed payload. `package-v25.ps1` already documents the exact .NET `0x80131515` / Mark-of-the-Web failure and instructs customers not to NETLOAD the raw downloaded payload. Because this remediation must happen before the managed DLL can load, adding a second in-plugin or parallel repair executable would duplicate the trust chain and cannot solve the pre-load failure.

The source change therefore stayed narrow and added:

- `QSUPDATE` as a short alias for the existing secure `QS3DUPDATE` Update Center;
- `QSVER` and `QS3DVER` commands reporting running product version, assembly version, the actual loaded DLL path, current updater state, and the GitHub Update Center command;
- focused auto-discovered source guard `scripts/preflight-v25-netload-update-ux.py` covering the command aliases, loaded-path diagnostics, existing `RemoteSigned`/Authenticode bootstrap, integrity/package identity checks, installed-payload unblocking, and package guidance.

No duplicate `repair-v25-netload.ps1` / `REPAIR-NETLOAD.cmd` was added, and the existing installer/package scripts were intentionally left unchanged after readback proved they already own the secure pre-load remediation.

## Completion evidence

- Claim registration: `21b6f0a2ff24555cef9bdcdd36f1830727018343` — `chore(agent): claim V25 NETLOAD update UX`
- Command implementation: `f540d72aa344e0ae68d08b2b6a4bd41e3adc62d2` — `fix(v25): harden NETLOAD install and update command UX`
- Focused regression/source guard: `ba2932267f1ca168cb9a043faa88b3b58ea49cc7` — `test(v25): guard NETLOAD and update command UX`

## Validation actually performed

- Re-fetched `UpdateCommands.cs` from current `main` and verified `QS3DUPDATE`, `QSUPDATE`, `QS3DVER`, `QSVER`, loaded `assembly.Location`, current version and updater-state reporting are present.
- Re-fetched the focused preflight from current `main` and verified it guards the existing secure installer/package Mark-of-the-Web path plus the new command surface.
- Re-read the existing updater lifecycle: plugin initialization starts the GitHub Release coordinator automatically; Update Center supports manual refresh and secure update scheduling.
- Re-read the existing package/install flow and confirmed installed payload files are unblocked only after integrity/package-identity validation.
- GitHub Actions were not dispatched; the owner requested implementation/commit/push, not CI or release publication.

## Remaining LOCAL_ONLY evidence

Exact licensed BricsCAD V25 clean-machine behavior — downloaded ZIP extraction, `INSTALL-QS3D.cmd`, DemandLoad/NETLOAD, Windows Authenticode/Mark-of-the-Web behavior, and update/restart behavior — remains native/local qualification. No remote runtime PASS is claimed.

## Coordination

The completed GitHub Release auto-update lane remains canonical for release discovery, signed-manifest eligibility, detached update handoff and restart. Recent NETLOAD startup/palette/ribbon lanes remain separate; this work did not modify those runtime lifecycle surfaces or V26.

## Completion condition

Satisfied for the remote/source lane: the new customer command surface and regression guard are on `main`, existing secure pre-load remediation was verified rather than duplicated, concurrent commits were preserved, and native V25 proof remains correctly local-only.
