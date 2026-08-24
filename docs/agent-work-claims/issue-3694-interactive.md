# Quantity Review measurement-trace lane

Status: ACTIVE
Lane-Key: issue-3694
Issue: #3694
Agent: interactive
Session: interactive-20260824-qrev8f3a
Baseline main: `eab17cb0727c428be3a6234d495cb86c107805e4`
Canonical branch: `agent/interactive-20260824-qrev8f3a/issue-3694-quantity-measurement-trace`
Canonical PR: pending
Supersedes: none

## Scope
- Add fail-closed exact-BREP face measurement operands for Quantity Review where length × height reconciles to the authoritative exact face area.
- Render the same trace in Quantity Insight and preserve it in the canonical QuantityExplanation/XLSX evidence graph.
- Preserve exact native face highlighting, deduction provenance, and stale/fail-closed behavior.

## Exclusions
- No second quantity engine and no persistent CAD presentation entities solely for review UI.
- No claim that #1669/#72 licensed BricsCAD runtime qualification is complete from source/CI evidence.

## Validation
- Deterministic Core smoke/source guard for measurement-trace validation and evidence operands.
- Protected PR preflight + core green on the exact candidate before merge.
- V25 compile when selected by CI; licensed viewport UX remains explicitly separate unless actually exercised.
