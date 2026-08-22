# Local delivery — 2026-08-10

This source snapshot is prepared as the recoverable handoff whenever GitHub publication is unavailable from this machine.

> **Product boundary:** every local delivery described here is a **BricsCAD V25 plugin** delivery. It contains/validates plugin DLLs and supporting files, not a standalone `QS3D.exe`. BricsCAD remains required for real runtime/NETLOAD/DemandLoad acceptance. See `docs/PRODUCT-BOUNDARY.md`.

## Reconciled source

- Reconciled remote baseline: `b00d03f` (`ci: auto discover all feature preflight gates`).
- Rebased B4D/ED2 commit: `5c24cb3` (`feat: add B4D scan and Excel handle round-trip`).
- The delivery also contains DWG fingerprint validation, generated-solid XData ownership, geometry dirty-state, far-origin footprint, current V25 compile fixes and regression hardening described in the canonical handoff.

## Local verification

- Core smoke suite: `ALL PASS`.
- BricsCAD V25.2.10 Release/x64 **plugin adapter** compile: 0 warnings, 0 errors.
- All twenty-one repository `preflight*.py` scripts: PASS.
- GitHub Actions were not dispatched.

## Safety and scope

- No BLT binary/source was decompiled or included.
- Private DWG/XLSX reference files were read-only and are not present in this delivery.
- The currently open user drawing was not modified, closed or used for NETLOAD testing.
- The ZIP is created outside the repository with a unique filename and does not overwrite an earlier delivery.
- Real NETLOAD/DemandLoad and interactive BricsCAD acceptance remain runtime-gated.
