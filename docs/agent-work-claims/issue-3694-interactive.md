# Quantity Review measurement-trace lane

Status: ACTIVE
Lane-Key: issue-3694
Issue: #3694
Agent: interactive
Session: interactive-20260824-qrev8f3a
Baseline main: `eab17cb0727c428be3a6234d495cb86c107805e4`
Reconciled main: `16610f51628b1db7491c7706eee92639ec736330`
Canonical branch: `agent/interactive-20260824-qrev8f3a/issue-3694-quantity-measurement-trace`
Canonical PR: #3695
Supersedes: none

## Scope
- Add fail-closed exact-BREP face measurement operands for Quantity Review where length × height reconciles to the authoritative exact face area.
- Render the same trace in Quantity Insight and preserve it in the canonical QuantityExplanation/XLSX evidence graph.
- Preserve face/intersection provenance, exact native face highlighting, and stale/fail-closed behavior.
- Unwrap BricsCAD V25 planar `ExternalBoundedSurface` BREP faces before planar classification/matching.

## Exclusions
- Do not create a second geometry or quantity engine.
- Do not create persistent CAD annotations/entities solely for review presentation.
- Do not claim that #1669/#72 licensed BricsCAD runtime qualification is complete from source/CI evidence.

## Validation
- Deterministic Core smoke covers exact face measurement validation, canonical operands, export projection and XLSX payload.
- `scripts/preflight-quantity-insight-measurement-trace.py` is auto-discovered by `preflight-all.py` and guards the V25/native-face/evidence/export seams.
- Protected PR `preflight` + `core` must be green on the exact current head before merge.
- V25 compile must pass through the pinned reference lane selected by CI; licensed viewport UX remains explicitly separate unless actually exercised.
