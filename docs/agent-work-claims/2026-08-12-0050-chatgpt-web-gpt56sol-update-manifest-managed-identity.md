# Work claim — update manifest managed identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-update-manifest-managed-identity`
- Registered: `2026-08-12T00:50:00+07:00`
- Completed: `2026-08-12T00:54:00+07:00`
- Baseline main SHA: `5d09620a6ea4654edabe95d3f683439093bb14bd`
- Priority: owner-requested continue-all review; align manifest producer identity with installer/finalizer consumers.

## Verified defect

`new-v25-update-manifest.ps1` verified signatures for both managed DLLs but bound `PACKAGE-METADATA` AssemblyVersion/productVersion only to `QS3D.BricsCAD.V25.dll`. A signed staging package whose `QS3D.Core.dll` carried a different managed identity could therefore receive a schema-v2 update manifest even though the hardened installer/finalizer reject the same package.

## Completed changes

- `6cf673768721d548fd8282f8128a03a5cf36c7bc` — generalized the manifest helper's managed version readers and now binds metadata AssemblyVersion/productVersion exactly to both signed `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` before ZIP/staging verification. Manifest version fields are taken from the plugin only after both DLLs prove the same canonical identity.
- `9aba7fdc8a408231ad2bb12fbe61eba25aeffaef` — extended `scripts/preflight-update-manifest-output-isolation.py` with plugin/Core identity models, required source tokens and ordering through ZIP binding/hash/manifest creation.
- `71fb3e95dc38ef4e2fb6504a931f276ef9dd50da` — documented dual managed identity for signed update-manifest generation.

## Validation evidence

- Inspected exact source diff for `6cf67376...`; changes are confined to generalizing the two managed version readers and replacing plugin-only binding with a two-DLL identity loop. Existing signer verification, output isolation, ZIP/staging byte equality, ZIP hash and schema-v2 fields are preserved.
- Re-fetched current `main` source blob `1755f6bc8e9dff19f527bd501bed46f2541d953d`; both managed DLL identities are checked before `Assert-ZipPayloadMatchesSignedStaging`.
- Re-fetched current regression blob `cdcf9ed3cd95a2f0f99209082ec8db11d797b65a`; it pins both managed identities before ZIP/hash/manifest mutation.
- Deterministic identity model: canonical plugin/Core identity PASS; Core AssemblyVersion mismatch FAIL; Core ProductVersion mismatch FAIL; case-only plugin ProductVersion mismatch FAIL.
- No signing, manifest publication, updater execution, package mutation, GitHub Release publication or BricsCAD runtime was executed in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

The ACTIVE updater generation-publication claim explicitly excludes manifest generation. No updater runtime source, package/finalizer/signing semantics, installer/uninstaller, workflow, `src/**`, `tests/**` or active product lane was modified. Output-isolation behavior from the preceding completed claim remains intact. All writes were SHA-guarded and no force-push was used.

## Result

The update-manifest producer now agrees with hardened installer/finalizer identity semantics: it cannot emit updater metadata for a signed package whose Core/plugin managed identities disagree with canonical package metadata. This lane is complete.
