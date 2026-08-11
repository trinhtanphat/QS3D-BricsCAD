# Work claim — update manifest output isolation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-update-manifest-output-isolation`
- Registered: `2026-08-12T00:44:00+07:00`
- Completed: `2026-08-12T00:49:00+07:00`
- Baseline main SHA: `87f28b33cc89cb7f7ab8576606ce1a5328ec31a3`
- Priority: owner-requested continue-all review; close a destructive manifest-generation boundary where arbitrary `OutputPath` could alias `PackageZip` or a file inside signed staging and later be overwritten by JSON after that exact artifact was verified.

## Completed changes

- `e59594aacb5187d57a527df86aa083836a153b31` — `scripts/new-v25-update-manifest.ps1` now normalizes the package root, package ZIP and output path; requires a `.json` output; rejects output equal to/inside signed staging; and rejects output equal to `PackageZip` before signer/staging/ZIP verification or mutation.
- `c3884320b1dac05d9312175800a458fef19e077b` — added auto-discovered `scripts/preflight-update-manifest-output-isolation.py` with Windows-path policy cases and source-order assertions.
- `3df37e80e4ee2c994cea6c55c3839c533bab272d` — documented signed update-manifest output isolation in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Inspected exact implementation diff for `e59594aa...`; only output-path normalization/guards moved ahead of existing verification and mutation. Signer checks, ZIP/staging byte equality, metadata/version checks, ZIP hash and manifest schema are otherwise unchanged.
- Re-fetched current `main` manifest source blob `cf8ad0d01f008f276e2dd6092ea8d81c5046dba7`; `.json`, package-tree and package-ZIP alias guards remain before package artifact verification/signature/ZIP binding.
- Re-fetched current regression blob `a10e553d7945bf029ff132123edf4642ddf3839c`; it pins isolation before signer/ZIP verification and `ShouldProcess`/`Set-Content`.
- Executed deterministic Windows-path policy model: sibling manifest PASS; external manifest PASS; nested staging manifest FAIL; staged metadata alias FAIL; package-ZIP alias FAIL; non-JSON output FAIL; similarly-prefixed sibling tree PASS.
- No signing, manifest publication, package mutation, GitHub Release publication or BricsCAD runtime was executed in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

The ACTIVE updater generation-publication claim explicitly excludes manifest generation. No `UpdateCoordinator.cs`, `SecureUpdateLauncher.cs`, `GitHubReleaseClient.cs`, updater selection/network code, installer/uninstaller, package/finalizer/signing semantics, workflow, `src/**`, `tests/**` or active product lane was modified. All writes were SHA-guarded and no force-push was used.

## Result

Manifest generation can no longer overwrite the package ZIP or a file inside signed staging after verifying it. Shareable updater metadata must be written to an external JSON path, preserving the exact signed package artifacts used to derive the manifest. This lane is complete.
