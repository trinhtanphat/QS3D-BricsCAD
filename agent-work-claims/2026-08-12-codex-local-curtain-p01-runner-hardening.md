# Work claim — LOCAL-002 Curtain P01 runner hardening

- Status: `COMPLETED`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `cd398601829106d2e4c1dc9b90398cf21297b14a`
- Priority: `LOCAL-002 / P0` — make the existing licensed P01 Curtain evidence process-clean, exact-SHA bound and safely repeatable before extending the remaining panel matrix

## Reserved scope

Harden only the existing automation runner and static evidence contract for the bounded `QS3DCURTAINPANELPROBE` P01 LINE/no-opening scenario. Require a clean exact Git SHA, an empty artifact directory outside the repository, truthful launched-process cleanup, deletion of the private `.scr` file, and sanitized metadata that remains tied to the exact candidate. Re-run the same P01 scenario on a fresh ordinary disposable DWG copy and update the canonical LOCAL-002 evidence without promoting P02-P12.

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

## Completion evidence

- Claim registration merged by PR `#685` at `9df191cf370b54ff1c1ab229e2e269994ab93ca6`.
- Runner hardening merged by PR `#691` at `d86b7146b7de439f6de30cc26a9600812a571f15`; the two-`git.exe` portability follow-up merged by PR `#693` at `846b8e172630e51bd2328b89028633d6f9b7d38d`.
- Exact clean runtime candidate: `53a4490f245774e9253d24ba70799b4311ff7e12`; BricsCAD `25.2.10` x64; plugin SHA-256 `644E24154161ADF0DE31A49E17FBB0FF65BB1B9A6251EC11594EA1E2E4924EAF`.
- P01 result: host/frame/panel counts `1/10/15`, metadata count `15`, disjoint ownership, Health issue count `0`, Locate/canonical owner counts `1/1`, complete panel build state, unchanged generated-sample DWG hash, no sidecar, deleted `.scr`, verified launched-process cleanup.
- PowerShell parse, `preflight-curtain-panel-runtime-probe.py`, `preflight-curtain-native-panels.py`, V25 x64 Release build and focused diff checks passed. No GitHub Actions were dispatched.
- LOCAL-002 remains `OPEN / PENDING_LOCAL`; P02-P12 are not covered by this completed lane.
