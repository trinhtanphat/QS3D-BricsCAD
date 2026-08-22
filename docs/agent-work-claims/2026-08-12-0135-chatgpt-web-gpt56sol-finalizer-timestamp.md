# Work claim — signed finalizer timestamp requirement

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-finalizer-timestamp`
- Registered: `2026-08-12T01:35:00+07:00`
- Priority: owner-requested continue-all review; make the signed-package finalizer independently enforce the release timestamp contract instead of relying only on workflow call ordering.

## Verified defect

`release-v25.yml` runs `verify-v25-signatures.ps1` before finalization and that verifier requires a timestamp certificate. However `finalize-v25-signed-package.ps1` itself accepts `Status=Valid` + signer thumbprint without requiring `TimeStamperCertificate`. Direct/alternate invocation can therefore finalize metadata/hash/ZIP for valid but untimestamped executable payloads, contrary to the production signed-release contract.

## Reserved scope

Require every executable payload signature verified by `finalize-v25-signed-package.ps1` to expose a timestamp certificate before any metadata/hash/ZIP mutation. Preserve exact signer thumbprint, dual managed identity, external ZIP isolation and all existing signature status checks. Extend the signed-finalizer preflight/model and release docs.

## Expected surfaces

- `scripts/finalize-v25-signed-package.ps1`
- `scripts/preflight-signed-finalizer-identity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- timestamp-server selection/network trust, certificate/key custody, signing helper, signature verification helper, workflow dispatch/publication, package/manifest/updater/installer, `src/**`, `tests/**`, licensed V25 runtime and V26 release surfaces.

## Validation plan

- Timestamp certificate requirement lives inside `Assert-AuthenticodeSigner` after `Status=Valid`/signer presence and before finalizer mutations.
- Regression policy model accepts valid expected signer + timestamp and rejects missing timestamp/wrong signer/invalid status.
- Static ordering keeps output isolation -> signature+timestamp -> metadata/plugin+Core identity -> `ShouldProcess` -> metadata/hash/ZIP mutation.
- No GitHub Actions dispatch/re-run or real signing/finalization.

## Completion condition

The finalizer cannot produce a signed final package from untimestamped executable payloads even when called outside the canonical workflow, with regression/docs on `main` and this claim `COMPLETED`.
