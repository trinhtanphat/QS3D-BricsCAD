# Quantity Review exact native face highlight lane

Status: ACTIVE
Lane-Key: issue-3480
Issue: #3480
Agent: gpt56sol
Session: quantity-review-exact-face
Baseline main: `b0635b0b2d16836850a564f2de00a9e43a2465e6`
Reconciled main: `b0635b0b2d16836850a564f2de00a9e43a2465e6`
Canonical branch: `agent/gpt56sol/3480-quantity-review-exact-face`
Canonical PR: #3487
Supersedes: none

## Scope
- Make every exact formwork BREP face row/value in Quantity Insight a model action.
- Revalidate the active DWG, canonical project/element, exact geometry fingerprint and exact face identity before locating.
- Resolve stable `SOLID-xx/FACE-yy` identities against the same ordered live Solid3d/BREP enumeration used by QuantityGeometryExplanationService.
- Highlight only the resolved native BricsCAD BREP subentity through `FullSubentityPath`; do not select/highlight the whole target solid for face actions.
- Clear the prior native face highlight on another face/action, tree/detail selection, panel unload and document switches.
- Preserve existing deduction target/cause selection plus transient exact intersection/contact preview unchanged.
- Add a feature source guard and runtime handoff/runbook.

## Exclusions
- No second geometry/takeoff engine.
- No persistent face color/material mutation.
- No licensed BricsCAD V25 runtime PASS can be claimed from source/CI alone.

## Validation
- PR metadata carries the literal unformatted `Lane-Key: issue-3480` required by the runtime collision parser.
- Feature source guard and aggregate preflight.
- Core build and deterministic smoke.
- Trusted BricsCAD V25 compile-reference validation and V25 plugin compile.
- Protected PR `preflight` + `core` must be green on the exact current head before merge.
- Licensed V25 interactive acceptance remains LOCAL_ONLY until executed on the exact merged candidate SHA.
