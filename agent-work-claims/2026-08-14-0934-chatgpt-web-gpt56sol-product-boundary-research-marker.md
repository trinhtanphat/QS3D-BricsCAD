# Work claim — Product-boundary research marker V25/V26 alignment

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / REMOTE_VERIFIED / PENDING_FRESH_AGGREGATE`
- Agent: `chatgpt-web-gpt56sol-product-boundary-research-marker-20260814-0934`
- Registered: `2026-08-14T09:34:00+07:00`
- Baseline main SHA: `0fad9bbd78db91ceec3cb22effa86d681c728383`
- Priority: `P0 aggregate-preflight regression` — product-boundary validation must reflect the current V25 + V26 hosted-plugin boundary rather than a stale V25-only research-doc literal.

## Confirmed defect

The exact-main aggregate preflight on `e98c30fb79abe41e0f9df6b5cd1d175152453675`, and the later #1099 validator aggregate after its own focused correction, reported `preflight-product-boundary.py` as failing. `scripts/preflight-product-boundary.py` describes its success contract as BricsCAD V25 + V26 managed Library plugins, but its `docs/BLT3D-RESEARCH.md` requirement still hard-coded `BricsCAD V25 plugin`. The research entry point correctly says `BricsCAD-hosted plugin` and points readers to the current V25/V26 sibling-product boundary. Reverting that doc to V25-only wording would regress canonical product truth.

## Reserved scope

- `scripts/preflight-product-boundary.py`
- this claim file

## Implemented acceptance

1. The gate still requires the BLT3D research doc's product-form clarification and clean-room workflow/UX-only boundary.
2. The stale V25-only research phrase was replaced by `BricsCAD-hosted plugin` plus explicit `current V25/V26` boundary markers.
3. All existing V25/V26 project Library and `IExtensionApplication` checks remain unchanged.
4. No product-form expansion and no production/native behavior changed.

## Explicit non-scope

No edits to `docs/BLT3D-RESEARCH.md`, product runtime code, V25 NETLOAD/update UX, wall-junctions, research implementation guard, or GitHub Actions.

## Completion record

- Claim-only commit: `a02f447d4d8b42944ffb9564ce8acdbf937f4983`.
- Guard fix: `fac9847b73d46dff6eb9c73d5cccec59cdf785a2` (`fix(preflight): align research boundary with V25 V26 product`).
- Remote read-back verified the `docs/BLT3D-RESEARCH.md` tuple now requires `Product-form clarification`, `BricsCAD-hosted plugin`, `current V25/V26`, and `workflow/UX only`.
- The current research document contains those markers and still links `docs/PRODUCT-BOUNDARY.md` for the canonical V25/V26 + sibling-product boundary.
- A concurrent #1099 validation merge retained `fac9847b...` in current-main ancestry and independently recorded that product-boundary remained one of only three unrelated preflight failures before this correction was available to a fresh aggregate rerun.
- Local execution: `NOT_RUN` in this connected GitHub environment.
- Fresh aggregate preflight: `PENDING_FRESH_AGGREGATE`.
- GitHub Actions: `NOT_DISPATCHED`.

## Completion

Source fix is complete and remotely verified. Future aggregate validation should use a descendant containing `fac9847b73d46dff6eb9c73d5cccec59cdf785a2`; the older product-boundary failure is stale for that corrected lineage.
