# Agent Work Claim — serialize uninstall with install/update

- Claim ID: `UNINSTALL-CROSS-ENTRY-LOCK-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:57:30+07:00`
- Baseline main SHA: `2839e2d5233e1142a3bcb7d2fa79a52b4dcec4bd`
- Parent lane: `UPDATER-MANUAL-CROSS-ENTRY-LOCK-20260811` (`RELEASED`)

## Verified defect

`update-v25.ps1` and `install-v25-autoload.ps1` now share the per-user updater mutex with the detached one-click worker, but `uninstall-v25-autoload.ps1` does not. Once BricsCAD is closed, direct uninstall can therefore remove DemandLoad registration and/or the install directory while an automatic/direct update or install owns the same mutation domain.

## Reserved scope

- `scripts/uninstall-v25-autoload.ps1`
- `scripts/preflight-update-cross-entry-lock.py` (extend the existing three-entry contract)
- `docs/UNINSTALL-CROSS-ENTRY-LOCK-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve uninstall custom-path identity protection, `-KeepFiles`, `-Force`, selected VersionKeys/LanguageKeys, `ShouldProcess`, and close-all-BricsCAD precondition.
- Preserve update/install mutex semantics, security gates and rollback behavior unchanged.
- Do not edit C# updater, manifest generator, release workflow or unrelated lanes.
- No Actions dispatch or release publication.

## Intended contract

1. Uninstall computes the same `Global\\QS3D-BricsCAD-V25-Update-<Windows SID>` mutex identity as auto/direct update/install.
2. It attempts immediate `WaitOne(0)`, recovers abandoned ownership, and fails fast on contention.
3. It holds ownership from before install-directory/registry inspection through selected DemandLoad removal and optional recursive file deletion.
4. Ownership is released/disposed exactly once from outer `finally`.
5. `-KeepFiles` still serializes registry mutation even when files are retained.
6. No process is force-terminated.

## Validation / release conditions

- Commit planning MD before source implementation.
- Extend `preflight-update-cross-entry-lock.py` so update/install/uninstall must all use the exact same SID/mutex contract and uninstall holds it through registry/file mutation.
- Re-fetch source/gate and verify ancestry with `behind_by: 0`.
- Actual contention behavior remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS claim.
- Release this claim only after source + gate are on `main`.