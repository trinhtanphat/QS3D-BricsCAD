# Solibri QA Gate 2.0 — structural identity severity floor

## Why this hardening exists

QA Gate 2.0 already treats three identity failures as structural and non-waivable:

- `QA2.DUPLICATE_ELEMENT_ID`
- `QA2.MISSING_IFC_GUID`
- `QA2.DUPLICATE_IFC_GUID`

Before this hardening, a custom `QsQaRuleProfile` could still assign one of those rules a severity below the blocking threshold. That allowed a structurally ambiguous model to pass even though an audit waiver could not release the same finding.

## Contract

Structural identity findings now have a fixed `Critical` floor inside `QsQaGate2`. Project severity overrides remain supported for ordinary completeness, property-set, relationship, spatial-consistency and type-consistency rules, but they cannot downgrade the three identity rules above.

The existing blocking-threshold API is unchanged. Because `Critical` is the highest supported severity, any valid threshold (`Info`, `Warning`, `Error`, or `Critical`) blocks when one of these structural findings is active. `CanTakeoff`, `CanBoq` and `CanEstimate` therefore remain false until the identity defect is corrected.

The same shared classifier is used for waiver exclusion and severity flooring so the two structural-identity policies cannot drift independently.

## Compatibility / migration

No public constructors, enums, rule IDs, finding DTOs, waiver DTOs, or guarded-executor APIs changed. Existing profiles remain source and binary compatible.

Configurations that attempted to downgrade `QA2.DUPLICATE_ELEMENT_ID`, `QA2.MISSING_IFC_GUID`, or `QA2.DUPLICATE_IFC_GUID` must stop relying on that behavior. Correct the element/IFC identity instead. If a project needs a softer policy, use configurable severities on non-structural QA rules rather than weakening identity uniqueness.

## Verification

`scripts/preflight-qa2-structural-identity-severity-floor.py` is auto-discovered by repository preflight and guards both sides of the invariant: the three rules share the structural classifier, and finding creation applies `QsQaSeverity.Critical` before any profile override can release them.
