# Work claim — update manifest timestamp requirement

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-update-manifest-timestamp`
- Registered: `2026-08-12T01:40:00+07:00`
- Priority: owner-requested continue-all review; make update-manifest generation independently enforce the timestamped-package release contract.

## Verified defect

`new-v25-update-manifest.ps1` validates Authenticode status and expected signer for both staged executable payloads and executable files extracted from the ZIP, but its signer helper does not require `TimeStamperCertificate`. Direct invocation can therefore emit a trusted schema-v2 update manifest for an otherwise correctly signed but untimestamped package even though production release policy requires timestamped payloads.

## Reserved scope

Require a timestamp certificate for every staged/zipped executable signature verified by the update-manifest generator before managed identity, ZIP hash or manifest output. Preserve output isolation, exact signer, plugin/Core identity, strict SemVer and ZIP↔staging byte equality. Extend the manifest preflight/model and runbook.

## Expected surfaces

- `scripts/new-v25-update-manifest.ps1`
- `scripts/preflight-update-manifest-output-isolation.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- updater runtime, signing helper, finalizer (already independently timestamp-hardened), signature verifier, workflow dispatch/publication, installer, `src/**`, `tests/**`, V26 files and licensed V25 runtime.

## Validation plan

- Timestamp requirement resides inside manifest generator `Assert-AuthenticodeSigner`, so it applies to both staging and ZIP-extracted executable verification.
- Policy model rejects missing timestamp/wrong signer/invalid status.
- Static ordering keeps output isolation -> signer+timestamp -> plugin/Core identity -> ZIP/staging binding -> ZIP hash -> manifest write.
- No GitHub Actions dispatch/re-run or real signing/manifest generation.

## Completion condition

A schema-v2 production update manifest cannot be generated for an untimestamped executable payload even outside the canonical workflow, with regression/docs on `main` and this claim `COMPLETED`.
