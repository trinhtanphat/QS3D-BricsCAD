# Work claim — Wall Junction preflight closed-polyline signature drift

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-preflight-signature-20260814-0936`
- Registered: `2026-08-14T09:36:00+07:00`
- Baseline main SHA: `706ed7bfc428f2115a7fa3decf2d815b2ba814d6`
- Priority: `P0 aggregate-preflight regression` — the wall-junction guard must follow the current safe closed-POLYLINE skip contract.

## Confirmed defect

Fresh aggregate validation from the #1099 validator reports `preflight-wall-junctions.py` as one of the final three unrelated failures. Production source commit `480dde3f1c0d018cfbf1c4a6638b3f254d7d42d9` intentionally changed `ReadSelection(...)` to return `out skippedClosedCount`, skip `polyline.Closed`, and report skipped closed polylines rather than treating closed structural/profile loops as wall centerlines. The preflight still requires the old `ReadSelection(document, selectedIds, sagitta, planarityTolerance)` call in both token and lifecycle assertions, so the safer source change trips a stale source-shape gate.

## Reserved scope

- `scripts/preflight-wall-junctions.py`
- this claim file

## Acceptance

1. Require the current `ReadSelection(..., out var skippedClosedCount)` call.
2. Guard `polyline.Closed`, `skippedClosedCount++`, and the closed-POLYLINE skip/report contract so the preflight protects the new behavior instead of merely accepting any signature drift.
3. Update lifecycle ordering to the current call while retaining selection-first/read-only-project/geometry-read/planning order.
4. Preserve all existing wall-junction topology, snap, finite/coplanar, UI and smoke checks.
5. No production wall code changes.

## Explicit non-scope

No edits to WallJunction production source/tests, product-boundary, research status, #1099 Update UX, native/local validation, or GitHub Actions.

## Validation plan

Publish claim alone, refresh `main`, patch only the stale wall-junction preflight markers, re-fetch the gate and current source, then close source fix. Fresh aggregate rerun remains separate evidence.
