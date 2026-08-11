# Work claim — quantity rule category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-rule-category-integrity-20260811-2225`
- Registered: `2026-08-11T22:25:00+07:00`
- Baseline main SHA: `c33038f685a5d27e107e3ba5659e6b8fe67781d3`
- Priority: deterministic Core invariant defect found during owner-requested `continue all` review

## Reserved scope

Reject undefined `ElementCategory` enum values when constructing a `QuantityRule`, so invalid rule state cannot enter `ProjectState` and remain inert until later persistence validation.

## Expected surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- No Quantity Settings UI/runtime rule resolution, intersection deduction/browser, reporting matrix, persistence implementation, formula engine, or native BricsCAD work.
- No changes to rule dependency ordering, expressions, outputs, provenance or regeneration semantics.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

`QuantityRule` canonicalizes required string fields but assigns `Category` without verifying the enum value is defined. An undefined value such as `(ElementCategory)999` can therefore become a live rule object and be added to `ProjectState.QuantityRules`; normal rule matching silently ignores it because no valid `ProjectElement` can have that category, while `QsdbProjectStore.ValidateProject` rejects the same state only later at save time. Other category-bearing domain objects already reject undefined values at construction/mutation.

## Validation plan

- Reject undefined category values in `QuantityRule` construction.
- Add focused workflow smoke coverage for invalid constructor rejection and preserve an existing valid Beam rule path.
- Re-fetch current `main` and both reserved files immediately before writes; use SHA-guarded writes due high concurrent branch movement.

## Coordination

Recent quantity-settings runtime-rule work is completed, and current quantity diagnostics/deduction lanes use disjoint source files. This claim does not reserve those surfaces.

## Completion condition

Source fix and regression are pushed to current `main`, the claim is closed with exact SHAs and actual validation limits, and no hosted/native test claim is made.
