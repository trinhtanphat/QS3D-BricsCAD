# Takeoff Package evaluation validation

This compatibility guard closes a P1 Autodesk Takeoff Package UX gap in the Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate workflow.

## Contract

`AutodeskTakeoffPackageCoordinator.Build` keeps argument/programmer validation unchanged. After source, revision, duplicate-evidence, and provenance validation succeeds, formula/rate evaluation is treated as a package publication boundary.

If the configured formula or rate provider fails, or an existing finite-value guard rejects its output, the coordinator now returns a `Blocked` package with `PKG.EVALUATION_FAILED`. No partial inventory is published and `CanEstimate` remains false.

Successful formula/rate evaluation is unchanged: measured quantities are grouped by classification/zone/unit, formulas are applied, rates are resolved, inventory is ordered deterministically, and the estimate remains available.

## Compatibility

Existing callers that intentionally relied on evaluation exceptions escaping from `Build` should inspect `TakeoffPackageBuildResult.Issues` instead. Fatal process failures such as `OutOfMemoryException` and `StackOverflowException` are not converted into business validation.

The change does not alter PDF/image ingestion, calibration, markup extraction, revision comparison, classification identity, duplicate drawing/BIM evidence rules, or estimate arithmetic.

## Validation

`QsTakeoffPackageEvaluationValidationSmoke` covers a throwing formula profile, a non-finite rate, zero partial-inventory publication on failure, and the unchanged successful estimate path. `preflight-takeoff-package-evaluation-validation.py` prevents removal of the package-level fail-closed contract.
