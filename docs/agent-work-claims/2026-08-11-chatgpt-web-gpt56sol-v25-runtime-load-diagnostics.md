# Work claim — V25 runtime load diagnostics

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol / v25-runtime-load-diagnostics`
- Registered: `2026-08-11T21:00:00+07:00`
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

## Validation plan

- Re-read current installer source and package contract before edits.
- Add a static regression preflight proving actionable process diagnostics, payload/registry checks, and documentation classification of font substitution versus plugin load failure.
- Execute the source-level preflight where possible in the remote environment.
- Re-fetch committed files and current `main` after each write; preserve concurrent commits.
- Record any Windows/BricsCAD-native load verification as LOCAL_ONLY instead of claiming remote proof.

## Coordination

The active GitHub auto-update claim owns update discovery/install trust-chain surfaces. This claim does not edit its allowed paths and treats the existing signed-update contract as immutable. No current claim filename indicates ownership of V25 install/runtime diagnostics or DWG font-substitution troubleshooting.

## Completion condition

Actionable V25 installer/runtime diagnostics, focused regression coverage, and font-substitution troubleshooting are pushed to `main`; native-only verification is explicitly handed off if still required; the claim is marked `COMPLETED` with commit/evidence details.
