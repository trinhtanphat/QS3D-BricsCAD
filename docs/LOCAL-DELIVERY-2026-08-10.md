# Local delivery — 2026-08-10

This source snapshot was prepared because `origin/main` was advancing continuously while the local Git credential flow was unavailable.

## Reconciled source

- Remote baseline: `904442c` (`test(preflight): guard typed editors and instance overrides`).
- Rebased B4D/ED2 commit: `645b399` (`feat: add B4D scan and Excel handle round-trip`).
- The packaged working tree also contains the uncommitted DWG fingerprint, generated-solid XData ownership, geometry dirty-state, far-origin footprint, current V25 compile and preflight hardening described in the canonical handoff.

## Local verification

- Core smoke suite: `ALL PASS`.
- BricsCAD V25.2.10 Release/x64 compile: 0 warnings, 0 errors.
- Generic, full-domain, Room lifecycle and geometry-completion preflights: PASS.
- GitHub Actions were not dispatched.

## Safety and scope

- No BLT binary/source was decompiled or included.
- Private DWG/XLSX reference files were read-only and are not present in this delivery.
- The currently open user drawing was not modified, closed or used for NETLOAD testing.
- Real NETLOAD/DemandLoad and interactive BricsCAD acceptance remain runtime-gated.
