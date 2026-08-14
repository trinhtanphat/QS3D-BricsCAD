# Work claim — Product-boundary research marker V25/V26 alignment

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-product-boundary-research-marker-20260814-0934`
- Registered: `2026-08-14T09:34:00+07:00`
- Baseline main SHA: `0fad9bbd78db91ceec3cb22effa86d681c728383`
- Priority: `P0 aggregate-preflight regression` — product-boundary validation must reflect the current V25 + V26 hosted-plugin boundary rather than a stale V25-only research-doc literal.

## Confirmed defect

The exact-main aggregate preflight on `e98c30fb79abe41e0f9df6b5cd1d175152453675` reported `preflight-product-boundary.py` as failing. Current `scripts/preflight-product-boundary.py` describes its own success contract as BricsCAD V25 + V26 managed Library plugins, but its `docs/BLT3D-RESEARCH.md` requirement still hard-codes `BricsCAD V25 plugin`. The research entry point now correctly says `BricsCAD-hosted plugin` and points readers to the current V25/V26 sibling-product boundary. Reverting that doc to V25-only wording would regress canonical product truth.

## Reserved scope

- `scripts/preflight-product-boundary.py`
- this claim file

## Acceptance

1. Keep requiring the BLT3D research doc's product-form clarification and clean-room workflow/UX-only boundary.
2. Require hosted-plugin wording plus an explicit V25/V26 boundary marker instead of the stale V25-only phrase.
3. Preserve all V25/V26 project Library and `IExtensionApplication` checks.
4. No product-form expansion and no production/native behavior changes.

## Explicit non-scope

No edits to `docs/BLT3D-RESEARCH.md`, product runtime code, V25 NETLOAD/update UX, wall-junctions, research implementation guard, or GitHub Actions.

## Validation plan

Publish claim alone, refresh `main`, patch only the stale requirement tuple, re-fetch gate + research doc, and close source fix. Fresh aggregate rerun remains separate evidence.
