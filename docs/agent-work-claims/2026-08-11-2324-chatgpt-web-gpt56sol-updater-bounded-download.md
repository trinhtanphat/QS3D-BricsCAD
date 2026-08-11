# Work claim — bounded final updater downloads

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:24:00+07:00`
- Completed: `2026-08-11T23:33:00+07:00`
- Baseline main SHA: `11486e07d818ec1df718f3775a4b3c23e15123da`
- Priority: owner-requested whole-repository audit; fail-closed detached updater availability/resource bound

## Verified defect

`scripts/update-v25.ps1` downloaded the final manifest and ZIP with `Invoke-WebRequest -OutFile`, then validated file size only after the transfer completed. The existing 64 KiB manifest and `MaxPackageSizeMB` package limits were therefore post-download checks rather than transfer bounds. The final detached worker also had no explicit transfer/read timeout at this layer.

After BricsCAD closed, a stalled response could hold the updater mutex and worker indefinitely without reaching the existing failure-restart path. A response larger than the configured maximum could consume arbitrary local disk before being rejected. This was an availability/resource-exhaustion gap even though hash, signer, SemVer, snapshot and archive integrity checks remained fail closed after download.

## Reserved scope

- `scripts/update-v25.ps1`
- `scripts/preflight-update-bounded-download.py`
- `docs/UPDATER-BOUNDED-DOWNLOAD-PLAN-2026-08-11.md`
- this claim file

## Non-overlap / preservation

- Preserve release-snapshot binding, schema/product/SemVer validation, publisher/signature pinning, package SHA-256, archive safety, stale installed-state checks, shared per-user update mutex and nested signed installer flow.
- Do not edit C# updater/UI, installer/uninstaller, manifest generator, release workflow, Grid annotation or GeneratedHandleOwnershipIndex lanes, or unrelated product surfaces.
- No GitHub Actions dispatch and no release publication.

## Completed contract

1. Final manifest and package transfers use shared `Invoke-BoundedHttpsDownload` instead of unbounded `Invoke-WebRequest -OutFile`.
2. The helper enforces explicit request/read-write timeouts, five automatic redirects maximum, requested HTTPS, and final-response HTTPS/no-userinfo validation.
3. Known `ContentLength` above the bound fails before copy; unknown/chunked responses are counted while streaming and fail before writing bytes beyond the caller limit.
4. Partial destination files are deleted on transfer/validation failure.
5. Manifest transfer is capped at 64 KiB with a 30-second timeout; package transfer is capped at the existing `MaxPackageSizeMB` limit with a 120-second timeout.
6. Existing post-download size, package SHA-256, archive, signature, release-snapshot, product-version and stale-installed-state gates remain intact.
7. `scripts/preflight-update-bounded-download.py` is auto-discovered by the aggregate preflight naming convention and guards the new source contract.

## Source/static evidence

- Source commit: `460fdda143afed1f63428b847042d8d8c10d49cc`.
- Regression gate commit: `fe0e7c78a31275486d818ca010030d527e812647`.
- Re-fetch confirmed current updater blob `ba1fa3e4e5c79f8eb61cd4bf7fccc8583d92fcb3` still contains the bounded helper and both bounded call sites.
- Compare from gate commit to current `main` at close-out returned `behind_by: 0`; concurrent commits did not touch updater/gate surfaces.
- Windows PowerShell/network behavior remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS is claimed.
