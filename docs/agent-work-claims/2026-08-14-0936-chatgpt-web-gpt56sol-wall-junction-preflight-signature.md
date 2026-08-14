# Work claim — Wall Junction preflight closed-polyline signature drift

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / REMOTE_VERIFIED / PENDING_FRESH_AGGREGATE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-preflight-signature-20260814-0936`
- Registered: `2026-08-14T09:36:00+07:00`
- Baseline main SHA: `706ed7bfc428f2115a7fa3decf2d815b2ba814d6`
- Priority: `P0 aggregate-preflight regression` — the wall-junction guard must follow the current safe closed-POLYLINE skip contract.

## Confirmed defect

Fresh aggregate validation from the #1099 validator reported `preflight-wall-junctions.py` as one of the final three unrelated failures. Production source commit `480dde3f1c0d018cfbf1c4a6638b3f254d7d42d9` intentionally changed `ReadSelection(...)` to return `out skippedClosedCount`, skip `polyline.Closed`, and report skipped closed polylines rather than treating closed structural/profile loops as wall centerlines. The preflight still required the old `ReadSelection(document, selectedIds, sagitta, planarityTolerance)` call in both token and lifecycle assertions, so the safer source change tripped a stale source-shape gate.

## Reserved scope

- `scripts/preflight-wall-junctions.py`
- this claim file

## Implemented acceptance

1. The gate now requires `ReadSelection(document, selectedIds, sagitta, planarityTolerance, out var skippedClosedCount)`.
2. It explicitly guards `if (polyline.Closed)`, `skippedClosedCount++`, and `closed POLYLINE`, so the safe skip/report contract is protected rather than merely tolerated.
3. Lifecycle ordering uses the current call while retaining selection-first → nonempty selection → read-only project lookup → geometry read → planning order.
4. Existing topology, snap, finite/coplanar, UI and smoke checks remain unchanged.
5. No production wall code changed.

## Explicit non-scope

No edits to WallJunction production source/tests, product-boundary, research status, #1099 Update UX, native/local validation, or GitHub Actions.

## Completion record

- Claim-only commit: `953f07d17256dde04e7142076c0f14abab0aaeed`.
- Guard fix: `106eb247a29ce8a417a645284520cc93a29675de` (`fix(preflight): follow closed polyline junction contract`).
- Remote read-back verified the current-call signature, closed-POLYLINE refusal markers, and updated lifecycle assertion are all present in `preflight-wall-junctions.py`.
- Current production `WallJunctionCommands.cs` contains the same `out skippedClosedCount` call, increments the counter for `polyline.Closed`, skips those entities, and reports skipped closed polylines to the user.
- The pre-fix #1099 aggregate had only product-boundary, research-status and wall-junctions remaining outside its own scope; all three now have source fixes on current-main lineage.
- Local execution: `NOT_RUN` in this connected GitHub environment.
- Fresh aggregate preflight: `PENDING_FRESH_AGGREGATE`.
- GitHub Actions: `NOT_DISPATCHED`.

## Completion

Source fix is complete and remotely verified. A capable fresh aggregate rerun should validate a descendant containing `fac9847b73d46dff6eb9c73d5cccec59cdf785a2`, `c093695b7b773b15d7671068341eb0edafe9df9f`, and `106eb247a29ce8a417a645284520cc93a29675de`; the older three-gate failure predates these corrections.
