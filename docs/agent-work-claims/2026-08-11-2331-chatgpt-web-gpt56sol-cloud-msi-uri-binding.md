# Work claim — cloud MSI URI object binding

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cloud-msi-uri-binding`
- Registered: `2026-08-11T23:31:00+07:00`
- Baseline main SHA: `be3e592a87e3f12e07731e4e147cbcd799a5135a`
- Priority: owner-requested whole-repository review; close a verified cloud release policy fail-open where the signed-secret fallback URL uses string-prefix matching even though policy allows only the pinned official MSI object's query string to differ.

## Reserved scope

Harden `.github/workflows/release-v25-cloud.yml` fallback MSI URL validation so a configured secret URL must resolve to the exact pinned official HTTPS scheme/host/port/path object, with only query parameters allowed to differ. Reject embedded credentials and fragments. Preserve the exact pinned SHA-256, Bricsys Authenticode signer and MSI ProductName/ProductVersion checks. Extend the existing cloud preview preflight and documentation.

## Expected surfaces

- `.github/workflows/release-v25-cloud.yml`
- `scripts/preflight-cloud-v25-preview-release.py`
- `docs/CLOUD-V25-PREVIEW-RELEASE.md`
- this claim file for close-out

## Excluded scope

- BricsCAD installer version/digest changes, mirror policy, Actions cache semantics or download ordering.
- manual self-hosted release workflow, updater/product code, signing keys, release publication behavior beyond fallback-URI validation.
- `src/**`, `tests/**`, active product lanes, GitHub Actions dispatch/re-run and licensed V25 runtime qualification.

## Validation plan

- Re-fetch exact workflow/preflight/doc blobs before writes and inspect source diffs.
- Regression model must allow exact pinned URL and query-only variants while rejecting path suffix/replacement, alternate host/scheme/port, credentials and fragments.
- Preserve digest verification before MSI extraction plus existing Authenticode/MSI identity checks.
- Execute Python regression with `python -S` in a synthetic fixture; no workflow dispatch.

## Coordination

Historical cloud installer pinning work is completed. Current-main active claims are in unrelated product/updater lanes; no current claim was found reserving the cloud fallback URI binding surface.

## Completion condition

The cloud fallback secret can differ from the pinned official MSI URL only by query string, regression/docs are updated on `main`, and this claim is marked `COMPLETED`.
