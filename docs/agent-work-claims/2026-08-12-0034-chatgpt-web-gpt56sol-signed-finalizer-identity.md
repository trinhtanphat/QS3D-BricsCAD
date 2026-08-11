# Work claim — signed finalizer package identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-signed-finalizer-identity`
- Registered: `2026-08-12T00:34:00+07:00`
- Completed: `2026-08-12T00:39:00+07:00`
- Baseline main SHA: `f5565546158d21391ded509d264fc86e3db3c486`
- Priority: owner-requested continue-all review; close a release-integrity gap where the signed package finalizer validated executable signatures and only the plugin AssemblyVersion, but did not re-bind PACKAGE-METADATA product/target/productVersion and Core identity before regenerating hashes/ZIP.

## Completed changes

- `435513f353f6ee66d0d1c9c21b1b484cb5740aad` — `scripts/finalize-v25-signed-package.ps1` now requires `PACKAGE-METADATA` product=`QS3D`, target=`BricsCAD V25 x64`, version and productVersion; after existing Authenticode checks it reads AssemblyVersion and ProductVersion from both signed `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` and requires exact metadata equality before `ShouldProcess` or any metadata/hash/ZIP mutation.
- `ad84e2d2c49adf33a0d5fc1df4e0098f71c1b043` — added auto-discovered `scripts/preflight-signed-finalizer-identity.py` with product/target/version/productVersion substitution and plugin/Core mismatch models plus source ordering assertions through metadata write, hash regeneration and ZIP compression.
- `e429d9d276682e19e75cdaa537140e80cae5111a` — documented signed-finalizer revalidation in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Inspected exact implementation diff for `435513f3...`; it only generalizes managed identity readers and adds canonical metadata + both-DLL identity binding. Existing signer checks, metadata enrichment fields, hash-generation logic and ZIP creation remain otherwise unchanged.
- Re-fetched current `main` finalizer blob `885c2bf1e04dccf70f04329fcff311ba97c36615`; signer verification precedes metadata checks, both signed managed DLLs are bound, and `ShouldProcess` remains before all package mutations.
- Re-fetched current preflight blob `e002d0ea6eede509a57a61bb99ecc63972435fff`; it pins signer -> identity -> ShouldProcess -> metadata/hash/ZIP ordering.
- Executed the deterministic identity model: canonical package PASS; product substitution FAIL; target substitution FAIL; Core AssemblyVersion mismatch FAIL; Core ProductVersion case mismatch FAIL.
- No Authenticode signing, package mutation, ZIP publication, GitHub Release publication or BricsCAD runtime was executed in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

No signing certificate/timestamp policy, package creation, updater/coordinator, installer/uninstaller, release workflow, product source under `src/**`, tests under `tests/**` or active product lane was modified. Concurrent `main` work was preserved with SHA-guarded writes and no force-push.

## Result

A metadata substitution between package creation/signing and finalization can no longer be turned into a finalized signed ZIP merely because executable signatures remain valid: canonical product/target and exact metadata identities must match both signed managed DLLs before finalization mutates or publishes package artifacts. This lane is complete.
