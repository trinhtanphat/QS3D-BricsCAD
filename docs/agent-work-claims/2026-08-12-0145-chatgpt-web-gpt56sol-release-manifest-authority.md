# Work claim — V25 release manifest authority coherence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release-manifest-authority`
- Registered: `2026-08-12T01:45:00+07:00`
- Priority: owner-requested continue-all review; complete the pre-publication signed-manifest coherence boundary beyond hash/URI alone.

## Verified defect

The pre-draft publication gate now re-binds signed manifest `sha256` and `packageUri` to the current ZIP, but does not revalidate authority fields such as schema/product/target/version/productVersion/signerThumbprint. A locally modified manifest can therefore remain byte-upload-consistent and point to the correct ZIP while advertising stale/wrong update identity metadata.

## Reserved scope

For signed V25 publication, read the unique root `PACKAGE-METADATA.json` from the **current ZIP** and require canonical package product/target/version/productVersion. Re-bind manifest schemaVersion/product/target/version/productVersion and signerThumbprint to that zipped metadata + expected signing thumbprint before draft creation. Extend the release publication preflight/model and runbook.

## Expected surfaces

- `.github/workflows/release-v25.yml`
- `scripts/preflight-release-asset-integrity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- V26; actual workflow dispatch/release; manifest generation helper; updater runtime; package/finalizer/signing/installer; existing hash/URI, tag-target and remote-asset gates; `src/**`; `tests/**`; licensed V25 runtime.

## Validation plan

- Open current ZIP read-only and require exactly one root `PACKAGE-METADATA.json` entry.
- Require zipped metadata product=`QS3D`, target=`BricsCAD V25 x64`, nonempty version/productVersion.
- Require signed manifest schemaVersion=2 and exact product/target/version/productVersion equality to zipped metadata.
- Normalize manifest signer thumbprint and require exact configured signer thumbprint.
- All checks occur before draft creation; archive/reader disposed in `finally`.
- Regression model rejects schema/product/target/version/productVersion/signer substitutions.
- No GitHub Actions dispatch/re-run.

## Completion condition

A signed V25 release draft cannot be created from a manifest whose update authority fields disagree with the exact current ZIP metadata/expected signer, with regression/docs on `main` and this claim `COMPLETED`.
