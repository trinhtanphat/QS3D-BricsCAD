# Work claim — uninstall force identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-uninstall-force-identity`
- Registered: `2026-08-12T00:30:00+07:00`
- Completed: `2026-08-12T00:35:00+07:00`
- Baseline main SHA: `ce8f8a7a02517cb944e4abe559bb65bd2748e129`
- Priority: owner-requested continue-all review; close a destructive uninstall boundary where `-Force` bypassed both custom-path scope and QS3D package identity, allowing an arbitrary existing directory to reach quarantine/recursive deletion.

## Completed changes

- `ab80d4a8549a8ee2cd23e221ce473eb12363d492` — `scripts/uninstall-v25-autoload.ps1` now treats `-Force` only as permission to use an intentional custom path outside the default QS3D LocalAppData scope. Every existing directory selected for file removal must contain `PACKAGE-METADATA.json`, plugin/Core DLLs, canonical `QS3D / BricsCAD V25 x64` metadata, a valid metadata AssemblyVersion/productVersion, and both managed DLLs must match those identities before quarantine staging.
- `7885e22e69a7b54ae7fea2228e48dc0be9cfa9a6` — `scripts/preflight-uninstall-transaction.py` now models default/custom/force/foreign cases, rejects the old force-gated identity pattern, requires metadata + both DLL identity checks, and pins validation before quarantine/registry mutation while retaining rollback assertions.
- `aba0e25ec4e7cf9af3380ffe2b2d013baf06ef32` — documented that forced uninstall never bypasses ownership validation.

## Validation evidence

- Inspected exact implementation diff for `ab80d4a8...`; changes are confined to `Assert-InstallDirectorySafeToRemove` plus the reconstructed EOF newline artifact. Transactional quarantine, registry snapshot/removal, rollback and process/mutex semantics were untouched.
- Re-fetched current `main` uninstall blob `3c28d6a60353d1f78150abe0f49fbaaba327b17e`; metadata/plugin/Core identity validation remains outside the `ForceDelete` scope check and occurs before the quarantine `Move-Item` call.
- Re-fetched current preflight blob `b0832c6ce1ce2b93170cf51663edd95f594c6f68`; it requires strong identity and explicitly rejects a return to force-bypassed identity validation.
- Executed the deterministic policy model: verified default install PASS; verified custom install without force FAIL; verified custom install with force PASS; foreign default/custom directories FAIL regardless of force.
- `-KeepFiles` remains a registry-only path because `Assert-InstallDirectorySafeToRemove` is called only under `if (-not $KeepFiles)`.
- No Windows filesystem/registry uninstall, BricsCAD runtime or private/customer data was exercised in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

Historical uninstall transaction/serialization claims were completed before this lane. The active updater generation-publication claim explicitly excluded installer/uninstaller scripts. No installer replacement, updater, package generation/finalization, signing, product source under `src/**`, tests under `tests/**` or active feature lane was changed. No force-push was used.

## Result

`-Force` no longer means “delete anything at this path.” It only authorizes a custom location after the target independently proves canonical QS3D V25 ownership. Foreign/non-QS3D directories cannot reach quarantine or recursive removal through this uninstall path. This lane is complete.
