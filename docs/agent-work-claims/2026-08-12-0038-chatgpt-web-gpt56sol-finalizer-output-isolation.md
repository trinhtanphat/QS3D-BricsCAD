# Work claim — signed finalizer output isolation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-finalizer-output-isolation`
- Registered: `2026-08-12T00:38:00+07:00`
- Completed: `2026-08-12T00:43:00+07:00`
- Baseline main SHA: `5cf18143b22a0bb2b340d1f28c6596546232aa31`
- Priority: owner-requested continue-all review; close a destructive finalizer boundary where arbitrary `PackageZip` could point inside the package payload (or at a non-ZIP payload file), which was later deleted by `Remove-Item` before compression.

## Completed changes

- `856a73f0671f868c2975df0402c68eddf30034e3` — `scripts/finalize-v25-signed-package.ps1` now normalizes the resolved package root, requires `PackageZip` to have a `.zip` extension, and rejects any output equal to or lexically beneath `PackageDirectory` before signer checks, identity checks, `ShouldProcess` or package mutation.
- `d78824301dcbb858c8a960d674e88dfebd949a13` — extended `scripts/preflight-signed-finalizer-identity.py` with Windows-path output-isolation models and source ordering through output cleanup/compression.
- `bd41c428cf8302d581fc943015ce7e498a21e4d5` — documented external ZIP output isolation in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Inspected exact implementation diff for `856a73f0...`; it only adds package-root normalization plus `.zip`/descendant guards before the existing signer/identity path. Existing metadata/hash/ZIP operations are otherwise unchanged.
- Deterministic Windows-path model results: sibling `QS3D-BricsCAD-V25.zip` PASS; nested `PackageDirectory\release.zip` FAIL; package payload-file target FAIL; non-ZIP output FAIL; similarly-prefixed sibling directory `QS3D-BricsCAD-V25-copy\release.zip` PASS.
- The regression pins output isolation -> signer verification -> metadata/DLL identity -> `ShouldProcess` -> metadata/hash mutation -> prior output removal -> compression ordering.
- Custom ZIP parent locations remain supported as long as the output is a `.zip` and is outside the package tree.
- No signing, package finalization, output deletion/compression, GitHub Release publication or BricsCAD runtime was executed in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

The preceding signed-finalizer identity claim was completed before this lane. No signing policy, package creation, updater/coordinator, installer/uninstaller, workflow, `src/**`, `tests/**` or active product lane was changed. All writes were SHA-guarded and no force-push was used.

## Result

`PackageZip` can no longer alias or sit inside the signed package tree, so finalizer output cleanup cannot delete a package DLL/manifest or create an archive recursively inside its own payload. This lane is complete.
