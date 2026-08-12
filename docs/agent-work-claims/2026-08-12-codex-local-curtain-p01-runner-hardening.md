# Work claim — LOCAL-002 Curtain P01 runner hardening

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `cd398601829106d2e4c1dc9b90398cf21297b14a`
- Priority: `LOCAL-002 / P0` — make the existing licensed P01 Curtain evidence process-clean, exact-SHA bound and safely repeatable before extending the remaining panel matrix

## Reserved scope

Harden only the existing automation runner and static evidence contract for the bounded `QS3DCURTAINPANELPROBE` P01 LINE/no-opening scenario. Require a clean exact Git SHA, a pre-existing empty artifact directory, truthful launched-process cleanup, deletion of the private `.scr` file, and sanitized metadata that remains tied to the exact candidate. Re-run the same P01 scenario on a fresh ordinary disposable DWG copy and update the canonical LOCAL-002 evidence without promoting P02-P12.

## Expected surfaces

- `scripts/test-bricscad-v25-curtain-panels.ps1`
- `scripts/preflight-curtain-panel-runtime-probe.py`
- `docs/CURTAIN-NATIVE-PANELS.md`
- `docs/LOCAL-AGENT-INBOX.md`
- The existing automation-only commands `QS3DCURTAINPANELPREPARE` / `QS3DCURTAINPANELPROBE` as read-only runtime dependencies; their product source is not reserved.

## Excluded scope

- No Curtain host/frame/panel geometry, clipping, fingerprints, ownership, transaction or health product-source changes.
- No P02-P12 implementation or qualification, failure injection, Undo/save/reopen or multi-DWG automation.
- No Level, Direct Draw, B4D/ED2, updater/release/signing, Ribbon/UI, Core domain/persistence or GitHub Actions changes.
- No modification of private owner DWGs or retention of raw paths, Handles, element IDs, project IDs, layers or customer content in committed evidence.

## Validation plan

- Re-fetch and verify this claim is an ancestor of current `origin/main` before editing the runner.
- Parse the PowerShell runner and pass `preflight-curtain-panel-runtime-probe.py` plus `preflight-curtain-native-panels.py`.
- Build `QS3D.BricsCAD.V25` x64/Release against installed BricsCAD V25 managed assemblies.
- Run P01 on a fresh ordinary disposable copy using an initialized local profile; verify exact Git SHA/plugin hash, 1 host, positive frame/panel counts, disjoint ownership, Health=0, Locate=1, unchanged DWG hash, no sidecar, deleted `.scr` and no surviving launched BricsCAD process.
- Commit only sanitized aggregate evidence; do not dispatch GitHub Actions.

## Coordination

Current ACTIVE/BLOCKED claims were rechecked at the baseline. None reserves the two runner/gate files or this bounded P01 requalification. The broader LOCAL-002 source implementation is already `REMOTE_DONE`; this claim intentionally does not reopen it or overlap the active Curtain frame negative-zero fingerprint claim.

## Completion condition

Runner/gate hardening and sanitized exact-SHA P01 evidence are merged to `main`, the claim is marked `COMPLETED`, and LOCAL-002 remains `OPEN / PENDING_LOCAL` for P02-P12.
