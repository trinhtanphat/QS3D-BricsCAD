# Work claim — bounded final updater downloads

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `11486e07d818ec1df718f3775a4b3c23e15123da`
- Priority: owner-requested whole-repository audit; fail-closed detached updater availability/resource bound

## Verified defect

`scripts/update-v25.ps1` downloads the final manifest and ZIP with `Invoke-WebRequest -OutFile`, then validates file size only after the transfer completes. The existing 64 KiB manifest and `MaxPackageSizeMB` package limits are therefore post-download checks rather than transfer bounds. The final detached worker also has no explicit transfer/read timeout at this layer.

After BricsCAD has closed, a stalled response can hold the updater mutex and worker indefinitely without reaching the existing failure-restart path. A response larger than the configured maximum can consume arbitrary local disk before being rejected. This is an availability/resource-exhaustion gap even though hash, signer, SemVer, snapshot and archive integrity checks remain fail closed after download.

## Reserved scope

- `scripts/update-v25.ps1`
- `scripts/preflight-update-bounded-download.py` (new)
- `docs/UPDATER-BOUNDED-DOWNLOAD-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve release-snapshot binding, schema/product/SemVer validation, publisher/signature pinning, package SHA-256, archive safety, stale installed-state checks, shared per-user update mutex and nested signed installer flow.
- Do not edit C# updater/UI, installer/uninstaller, manifest generator, release workflow, Grid annotation or GeneratedHandleOwnershipIndex lanes, or unrelated product surfaces.
- No GitHub Actions dispatch and no release publication.

## Intended contract

1. Final manifest and package transfers use one bounded HTTPS download helper instead of unbounded `Invoke-WebRequest -OutFile`.
2. The helper enforces explicit connect/response read timeouts and a maximum redirect count while requiring HTTPS on both requested and final response URI.
3. Known `ContentLength` above the bound fails before copying; unknown/chunked responses are counted while streaming and fail immediately once the byte bound is exceeded.
4. Partial destination files are deleted on any transfer/validation failure.
5. Manifest transfer is capped at 64 KiB; package transfer is capped at the existing `MaxPackageSizeMB` limit.
6. Existing post-download size/hash/archive/signature/version gates remain as defense in depth.

## Validation / release conditions

- Commit a planning MD before implementation.
- Add an auto-discovered static regression proving timeout/redirect/HTTPS/stream-byte bounds, partial cleanup, and replacement of both final `Invoke-WebRequest -OutFile` calls.
- Re-fetch exact source/gate and verify ancestry with `behind_by: 0` before closing.
- Windows PowerShell/network behavior remains `LOCAL-009 / PENDING_LOCAL`; do not claim remote runtime PASS.
- Mark this claim `COMPLETED` only after source + regression gate are committed on `main`.