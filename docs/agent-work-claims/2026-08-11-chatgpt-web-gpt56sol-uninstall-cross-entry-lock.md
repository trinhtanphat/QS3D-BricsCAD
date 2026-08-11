# Agent Work Claim — serialize uninstall with install/update

- Claim ID: `UNINSTALL-CROSS-ENTRY-LOCK-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:57:30+07:00`
- Released: `2026-08-11T23:00:30+07:00`
- Baseline main SHA: `2839e2d5233e1142a3bcb7d2fa79a52b4dcec4bd`
- Parent lane: `UPDATER-MANUAL-CROSS-ENTRY-LOCK-20260811` (`RELEASED`)

## Verified defect

`update-v25.ps1` and `install-v25-autoload.ps1` shared the per-user updater mutex with the detached one-click worker, while `uninstall-v25-autoload.ps1` did not. Direct uninstall could therefore remove DemandLoad registration/files while another install/update owned the same mutation domain.

## Completed changes

- `de9c9d15ed6c203a91edb8f61007b543dccfd25a` — registered this follow-up lane before implementation.
- `227c7259b35879ab76c3b09f4129209560156d4f` — committed uninstall serialization planning before source changes.
- `43fb6e81f6addb11103b278837b526bc1457bf6a` — uninstall now resolves the same Windows SID and `Global\\QS3D-BricsCAD-V25-Update-<SID>` mutex, uses nonblocking `WaitOne(0)`, recovers abandoned ownership, fails fast on contention, and holds the lane from before safe-path/registry inspection through DemandLoad removal and optional file deletion.
- `3280a97bfe622aeb58c94a945be5e077c6f5be49` — extended `preflight-update-cross-entry-lock.py` so secure update, install and uninstall must all share the exact mutex contract and maintain ordered lock lifetimes.

## Preserved behavior

- Uninstall still refuses while any BricsCAD process is running.
- Custom install directories outside the QS3D LocalAppData scope remain protected unless `-Force` is explicit.
- Non-forced recursive removal still requires valid QS3D V25 package identity markers.
- `VersionKeys`, `LanguageKeys`, `KeepFiles`, `Force`, and `ShouldProcess` semantics remain intact.
- No process termination was added.

## Integration verification

- Re-fetched uninstall source after implementation and inspected the shared mutex helper + outer `try/finally` lifetime.
- Compare from `3280a97bfe622aeb58c94a945be5e077c6f5be49` to current `main` reported `behind_by: 0`; the only later change at verification time was an unrelated Core smoke test.
- The cross-entry gate is auto-discovered by `preflight-all.py`.
- No GitHub Actions workflow was dispatched and no release was published.

## Validation boundary

Source/static serialization now covers automatic/direct update, install and uninstall. Actual Windows contention, recursive ownership and filesystem/registry behavior remain `LOCAL-009 / PENDING_LOCAL`; this lane does not claim native/runtime PASS.