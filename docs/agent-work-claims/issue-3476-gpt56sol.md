# Quantity Review canonical evidence/navigation lane

Status: ACTIVE
Lane-Key: issue-3476
Issue: #3476
Agent: gpt56sol
Session: quantity-review-evidence-nav
Baseline main: `4e27ced6bb1519610921ed8470f02fc32796db0d`
Reconciled main: `e3b27191d32504256ae113b457e37c5031937b96`
Canonical branch: `agent/gpt56sol/3476-quantity-review-evidence-nav`
Canonical PR: #3477
Supersedes: none

## Scope
- Upgrade Quantity Insight navigation to Floor -> Type/category -> Name/family -> individual Element.
- Project existing exact `QuantityGeometryExplanation` into the canonical Core `QuantityExplanation` evidence contract without re-running geometry.
- Preserve face/intersection selector provenance and stable evidence IDs in XLSX evidence projection.
- Add current-review XLSX evidence export from Quantity Insight using the revalidated exact geometry snapshot.
- Add deterministic Core smoke coverage.

## Exclusions
- Do not replace or modify #1669 native BREP measurement, face/subentity highlight, transient-region implementation, or licensed runtime qualification.
- Do not create a second geometry or quantity engine.
- Licensed BricsCAD V25/V26 runtime remains #72/#1669.

## Validation
- Core build and deterministic smoke.
- Shared branch CI on exact branch SHA before PR.
- Trusted V25 compile reference validation and BricsCAD V25 plugin compile when selected by CI.
- Protected PR `preflight` + `core` must be green on the exact current head before merge.
