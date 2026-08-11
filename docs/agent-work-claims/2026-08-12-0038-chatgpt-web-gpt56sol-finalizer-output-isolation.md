# Work claim — signed finalizer output isolation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-finalizer-output-isolation`
- Registered: `2026-08-12T00:38:00+07:00`
- Baseline main SHA: `5cf18143b22a0bb2b340d1f28c6596546232aa31`
- Priority: owner-requested continue-all review; close a destructive finalizer boundary where arbitrary `PackageZip` can point inside the package payload (or at a non-ZIP payload file), which is later deleted by `Remove-Item` before compression.

## Reserved scope

Harden `scripts/finalize-v25-signed-package.ps1` so the finalized output must be a `.zip` path outside `PackageDirectory`; reject equality/descendant paths before `ShouldProcess` and before any metadata/hash/output mutation. Extend `scripts/preflight-signed-finalizer-identity.py` with output-isolation policy/model assertions and align release docs.

## Expected surfaces

- `scripts/finalize-v25-signed-package.ps1`
- `scripts/preflight-signed-finalizer-identity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- finalizer metadata/DLL identity semantics already completed, signing certificate/timestamp policy, package creation, updater/coordinator, installer/uninstaller, workflow dispatch/publication, `src/**`, `tests/**`, active product lanes and licensed V25 runtime.

## Validation plan

- Require normalized package-root boundary and `.zip` extension before package mutation.
- Reject any output equal to or lexically beneath `PackageDirectory`, including a path matching an existing package DLL/manifest.
- Preserve custom ZIP parent support outside the package tree and existing `ShouldProcess`/signer/identity logic.
- Regression model covers sibling ZIP PASS, nested ZIP FAIL, payload-file target FAIL, non-ZIP FAIL and similarly prefixed sibling path PASS.
- No GitHub Actions dispatch/re-run.

## Coordination

The preceding signed-finalizer identity claim is completed. No concurrent claim was found for finalizer output path isolation; active product claims remain outside release helper surfaces.

## Completion condition

Finalizer output cannot alias or reside inside the package tree and must be a ZIP path before destructive output cleanup/compression, with regression/docs on `main` and this claim marked `COMPLETED`.
