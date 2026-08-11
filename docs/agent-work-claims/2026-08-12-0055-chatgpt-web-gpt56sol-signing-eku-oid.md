# Work claim — signing EKU OID validation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-signing-eku-oid`
- Registered: `2026-08-12T00:55:00+07:00`
- Baseline main SHA: `cc830c7f7d87c73738df48f0b4dedbed2e72edbf`
- Priority: owner-requested continue-all review; remove locale/rendering dependence from the production code-signing certificate EKU gate.

## Verified defect

`scripts/sign-v25.ps1` locates the Enhanced Key Usage extension by OID but calls `Format($true)` and then accepts/rejects based on rendered text containing either the code-signing OID or the English words `Code Signing`. X509 extension display text is presentation data and can vary by platform/locale; a valid code-signing certificate may therefore be falsely rejected on a non-English/alternate Windows renderer.

## Reserved scope

Validate an existing EKU extension from its structured `X509EnhancedKeyUsageExtension.EnhancedKeyUsages` OID collection and require OID `1.3.6.1.5.5.7.3.3`. Preserve current semantics that a certificate without an EKU extension is not rejected by this helper, and preserve CurrentUser store, private-key, validity, timestamp HTTPS, SHA-256 signing and post-sign signer checks. Add an auto-discovered static/model regression and document the certificate gate.

## Expected surfaces

- `scripts/sign-v25.ps1`
- `scripts/preflight-signing-eku.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- certificate/key custody, store location policy, timestamp trust semantics, signature verification helper, package/finalizer/manifest/updater/installer workflows, `src/**`, `tests/**`, workflow dispatch/publication and licensed V25 runtime.

## Validation plan

- No `Format(...)`/localized friendly-name dependency in EKU authorization.
- If EKU extension exists, cast/copy to `X509EnhancedKeyUsageExtension` and enumerate `EnhancedKeyUsages` OID values; require exact code-signing OID.
- Regression model: no EKU preserves current acceptance; Code Signing OID PASS; unrelated Client Auth/Server Auth OIDs FAIL; friendly/localized labels are irrelevant.
- Pin EKU check before any `Set-AuthenticodeSignature` invocation.
- No GitHub Actions dispatch/re-run or real certificate signing.

## Coordination

No current signing-helper claim was found. Recent release-helper claims for finalizer/manifest are completed and are outside this signing certificate-selection scope.

## Completion condition

Signing EKU authorization is OID-structural and locale-independent before signing starts, with regression/docs on `main` and this claim marked `COMPLETED`.
