# Local delivery — 2026-08-10

This source snapshot is prepared as the recoverable handoff whenever GitHub publication is unavailable from this machine.

## Reconciled source

- Reconciled remote baseline: `3a319e6` (`feat(health): add unified generated rebar health command`).
- Rebased B4D/ED2 commit: `fcf1fe6` (`feat: add B4D scan and Excel handle round-trip`).
- The delivery also contains DWG fingerprint validation, generated-solid XData ownership, geometry dirty-state, far-origin footprint, current V25 compile fixes and regression hardening described in the canonical handoff.

## Local verification

- Core smoke suite: `ALL PASS`.
- BricsCAD V25.2.10 Release/x64 compile: 0 warnings, 0 errors.
- All fourteen repository `preflight*.py` scripts: PASS.
- GitHub Actions were not dispatched.

## Safety and scope

- No BLT binary/source was decompiled or included.
- Private DWG/XLSX reference files were read-only and are not present in this delivery.
- The currently open user drawing was not modified, closed or used for NETLOAD testing.
- The ZIP is created outside the repository with a unique filename and does not overwrite an earlier delivery.
- Real NETLOAD/DemandLoad and interactive BricsCAD acceptance remain runtime-gated.
