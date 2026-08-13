# Work claim — V25 NETLOAD bootstrap + update command UX

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T22:18:00+07:00`
- Baseline main SHA: `c33f8404cb0fc66641741970b23a9cd5b6d5e03d`
- Priority: Owner-reported V25 `NETLOAD` failure with HRESULT `0x80131515` / downloaded-package dependency load failure, plus request for direct GitHub update commands.

## Reserved scope

Harden the V25 customer package so a downloaded/extracted package has an explicit integrity-checked Mark-of-the-Web repair path for intentional direct `NETLOAD`, make the one-click installer safely support reinstall/upgrade over an existing valid QS3D registration, and expose short update/version command aliases while preserving the existing secure GitHub Release update center and signed one-click update trust chain.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs`
- `scripts/INSTALL-QS3D.cmd`
- `scripts/package-v25.ps1`
- new `scripts/repair-v25-netload.ps1`
- new `scripts/REPAIR-NETLOAD.cmd`
- new focused `scripts/preflight-v25-netload-update-ux.py`
- this claim file for completion evidence

## Excluded scope

- no V26 updater/package changes
- no release version bump, tag creation, GitHub Release publication, or Actions dispatch
- no changes to GitHub update endpoint/channel/signature/manifest trust policy
- no BricsCAD security-setting weakening and no execution-policy bypass
- no unrelated NETLOAD startup/palette/ribbon runtime lane changes
- no claim of licensed V25 runtime PASS from remote source work

## Validation plan

- preserve package SHA256 verification before in-place unblocking
- require the repair launcher to use `RemoteSigned`, never `Bypass`
- repair the complete verified package dependency folder rather than only the primary DLL
- ensure `INSTALL-QS3D.cmd` remains signature-aware and invokes the atomic installer with intentional upgrade semantics
- expose `QSUPDATE` as an alias of `QS3DUPDATE` and add `QSVER`/`QS3DVER` version reporting from the running assembly identity
- add an auto-discovered focused source preflight covering these contracts
- re-fetch every changed file from current `main` after writes; do not dispatch Actions

## Coordination

The existing GitHub Release auto-update lane is already completed/released and remains the canonical secure updater implementation. This claim only adds customer-facing aliases/bootstrap repair/reinstall UX around it. Recent NETLOAD startup hang/ribbon/palette claims own runtime lifecycle behavior and are explicitly excluded.

## Completion condition

A coherent source/scripts/preflight batch is pushed to current `main`, read back from GitHub, this claim is marked `COMPLETED` with exact commit evidence, and licensed V25 runtime/clean-machine proof remains correctly classified as local-only if still pending.
