# Work claim — V25 runtime load diagnostics

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol / v25-runtime-load-diagnostics`
- Registered: `2026-08-11T21:00:00+07:00`
- Completed: `2026-08-11T21:22:00+07:00`
- Baseline main SHA: `1e8227e72ff54c1fd6daf4c32121b09339370d0b`
- Priority: user-reproduced V25 installation/load failure plus misleading drawing-font warnings in the current support session

## Reserved scope

Harden the existing BricsCAD V25 install/runtime support path so user-visible failures distinguish an open BricsCAD process, missing or mismatched host/plugin files, registry DemandLoad registration, Windows download blocking evidence, and unrelated DWG font-substitution warnings. Add source-level regression coverage and a focused troubleshooting runbook. Do not claim that remote/source inspection can reproduce native BricsCAD loading.

## Expected surfaces

- `scripts/install-v25-autoload.ps1`
- new focused V25 runtime/install diagnostic preflight under `scripts/`
- new focused troubleshooting documentation under `docs/`
- `docs/LOCAL-AGENT-INBOX.md` only if a native BricsCAD-only verification gate must be recorded
- packaged installer behavior already sourced from `scripts/install-v25-autoload.ps1`; no updater trust-chain redesign

## Excluded scope

- GitHub release auto-update UI, manifest, detached updater, signing/publisher trust-chain work owned by `2026-08-11-chatgpt-web-gpt56sol-github-auto-update.md`
- `.github/workflows/release-v25.yml` and `scripts/package-v25.ps1`
- `src/QS3D.Core/**`, Ribbon, Start Center, Workspace, Quantity/BOQ, Room/Family implementation lanes
- bundling or redistributing third-party/proprietary VNI/SHX font files
- changing DWG text styles automatically or silently suppressing BricsCAD font warnings

## Implemented

- `339d9b762d5021450c19186066be565d20c225b7` — added `docs/V25-RUNTIME-TROUBLESHOOTING.md`, separating DWG font substitution (`vntimeh.shx` / `VNI-Times` -> `simplex.shx`) from QS3D assembly load failures, documenting the V25 Pro-or-higher host boundary, DemandLoad values, and safe local diagnosis.
- `8bfb74a049083105d58d65ecdd9ef74739050fc4` — added auto-discovered `scripts/preflight-v25-runtime-diagnostics.py` to gate the support/runtime diagnostic contract.
- `9b0bc0ea56b759e7f92c63ceaf3af27b1d46d524` — hardened `scripts/install-v25-autoload.ps1`: running BricsCAD failures now report process name/PID/path when available; the installer explicitly warns that QS3D requires V25 Pro or higher; DemandLoad writes are read back and validated for Loader, LoadCtrls, Description, and every packaged command; mismatches fail into the existing rollback path. Existing `Unblock-File` payload handling and package integrity/signing behavior were preserved.

## Validation evidence

- Re-fetched `scripts/install-v25-autoload.ps1` from current `main` after the write; committed blob `f6fe92f5144c4f8eb393a35f664a6c84b3cd1fc9` contains the process diagnostics, Pro-or-higher warning, preserved `Unblock-File`, and DemandLoad readback gate.
- Bricsys V25 documentation confirms registry DemandLoad values `2 = OnStartup` and `4 = OnCommand`, matching the installer contract.
- Bricsys V25 BRX documentation confirms the BRX/.NET API is available only for BricsCAD Pro and higher license levels.
- `main` advanced concurrently after the installer commit; compare evidence showed `9b0bc0e...` remained the merge base/ancestor and was not overwritten.
- GitHub reported no workflow run attached directly to `9b0bc0e...`. The remote execution environment has neither Windows/PowerShell nor licensed BricsCAD V25, so no native runtime PASS is claimed here.
- The repository already has the matching native exact-V25 build/NETLOAD/DemandLoad/runtime lane under `docs/LOCAL-AGENT-INBOX.md` item `LOCAL-001`; this batch does not create a duplicate LOCAL_ONLY queue item. The next exact-candidate local qualification should include the new installer diagnostics and the font-warning/non-plugin-failure classification.

## Coordination

The active GitHub auto-update claim owns update discovery/install trust-chain surfaces. This claim did not edit its allowed paths and treated the existing signed-update contract as immutable.

## Completion condition

Completed: actionable V25 installer/runtime diagnostics, focused regression coverage, and font-substitution troubleshooting are pushed to `main`; native-only verification remains explicitly owned by existing `LOCAL-001` and is not misrepresented as a remote PASS.
