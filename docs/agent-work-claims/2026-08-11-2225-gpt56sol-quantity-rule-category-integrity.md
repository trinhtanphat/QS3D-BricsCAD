# Work claim — quantity rule category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-rule-category-integrity-20260811-2225`
- Registered: `2026-08-11T22:25:00+07:00`
- Completed: `2026-08-11T22:30:30+07:00`
- Baseline main SHA: `c33038f685a5d27e107e3ba5659e6b8fe67781d3`
- Claim commit: `12524e100f54fb46b0875598eb27200363d78b20`
- Implementation commit: `af9fb7b6bcd17a9de2d7647eb3410ed3f9871739`
- Regression-test commit: `e2ce150bb1e2b27dbbf92fcffdc2f5c30acff86b`
- Priority: deterministic Core invariant defect found during owner-requested `continue all` review

## Reserved scope

Reject undefined `ElementCategory` enum values when constructing a `QuantityRule`, so invalid rule state cannot enter `ProjectState` and remain inert until later persistence validation.

## Implemented

- `QuantityRule` now rejects undefined category values at construction with `ArgumentOutOfRangeException`.
- Existing string-field canonicalization and all rule dependency/output/provenance behavior remain unchanged.
- `WorkflowPersistenceSmoke` now locks invalid category rejection immediately before the existing valid Beam rule-driven regeneration path, preserving the normal-path regression in the same smoke method.

## Changed surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs`
- this claim file

## Excluded scope

- No Quantity Settings UI/runtime rule resolution, intersection deduction/browser, reporting matrix, persistence implementation, formula engine, or native BricsCAD work.
- No changes to rule dependency ordering, expressions, outputs, provenance or regeneration semantics.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

Before the fix, `QuantityRule` assigned `Category` without checking whether the enum value was defined. An undefined value such as `(ElementCategory)999` could become a live rule and be added to `ProjectState.QuantityRules`; normal rule matching silently ignored it because valid `ProjectElement` instances cannot carry that category, while `QsdbProjectStore.ValidateProject` rejected the same invalid state only later during save. Other category-bearing domain objects already fail closed at construction/mutation.

## Validation performed

- Published claim `12524e100f54fb46b0875598eb27200363d78b20` on current `main` before product changes.
- Re-fetched both reserved files and used exact current blob SHAs for conflict-safe writes.
- Source fix committed as `af9fb7b6bcd17a9de2d7647eb3410ed3f9871739`; regression source committed as `e2ce150bb1e2b27dbbf92fcffdc2f5c30acff86b`.
- Compared claim commit to then-current `main` `64a78875be70a93091f4a4dcdd2446c219b68ab3`: status `ahead`, `ahead_by=12`, `behind_by=0`; both changed source/test files remained in ancestry.
- A later `quantity-rule-create-command` lane appeared in a disjoint native command file; it did not overwrite this Core rule/test change.
- No GitHub Actions workflow was dispatched or re-run. This remote pass does not claim hosted smoke execution or BricsCAD V25 runtime qualification.

## Outcome

Invalid quantity-rule categories now fail at the rule object boundary rather than entering project state and surfacing only as a later persistence failure or inert rule.
