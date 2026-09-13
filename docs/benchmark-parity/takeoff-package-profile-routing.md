# Autodesk Takeoff Package profile-aware evaluation

## Contract

`TakeoffPackageDefinition` owns the package-selected `ClassificationProfile` and `FormulaProfile`. The profile-aware package path now carries both selections into formula and rate evaluation across:

`Drawing/BIM -> Package -> Classification -> Quantity -> Formula -> Inventory -> Estimate`

Use `AutodeskTakeoffPackageCoordinator.BuildProfileAware(...)` when formula/rate resolution depends on the package-selected profiles. The evaluator receives the package classification profile, formula profile, resolved classification, and quantity/unit at the same evaluation boundary used by the existing coordinator.

## Compatibility

The existing `AutodeskTakeoffPackageCoordinator.Build(...)` overload is unchanged. Callers whose formula/rate logic is already bound to a profile can continue using it without migration.

`BuildProfileAware(...)` is an additive extension API. It adapts profile-aware evaluators into the existing coordinator, so sheet/revision/evidence validation, duplicate guards, BIM quantity validation, inventory construction, compensated estimate aggregation, and readiness semantics remain authoritative in one place.

## Failure behavior

Exceptions or non-finite outputs raised by profile-aware evaluators flow through the coordinator's existing fail-closed evaluation boundary. The package reports `PKG.EVALUATION_FAILED`, becomes `Blocked`, publishes no partial inventory/estimate, and cannot be estimated.

## Migration guidance

Package UIs that let users select a classification table, formula book, workbook profile, or estimating policy should prefer `BuildProfileAware(...)` and resolve their formula/rate providers using the two package profile identifiers passed to the evaluator. Do not read mutable ambient UI selection inside evaluator callbacks; the package definition is the deterministic selection snapshot for the build.
