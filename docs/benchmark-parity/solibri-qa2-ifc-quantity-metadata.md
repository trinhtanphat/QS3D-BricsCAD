# Solibri QA Gate 2.0 — IFC quantity metadata completeness

## Purpose

The strict QA2 profile now aligns with the pre-existing strict IFC quantity contract by requiring both `IfcEntity` and `QuantityUnit` metadata in addition to IFC GUID identity, Pset, relationship, material/type/classification, spatial, and dimension checks.

## Rules

- `QA2.MISSING_IFC_ENTITY` is emitted when `IfcEntity` is absent, empty, or whitespace-only.
- `QA2.MISSING_QUANTITY_UNIT` is emitted when `QuantityUnit` is absent, empty, or whitespace-only.
- `SolibriQuantityStrict()` assigns both rules `Error` severity. With the default `Error` blocking threshold, either finding blocks Takeoff, BOQ, and Estimate through the existing QA2 guarded executor.
- These are completeness findings, not structural identity conflicts. Their severity remains profile-configurable and they continue to support the existing audited waiver mechanism.

## Compatibility and migration

Existing adapters that already satisfy `QsQaProfile.StrictIfcQuantity()` are compatible because that profile has long required `IfcEntity` and `QuantityUnit`. Adapters that only populated QA2-specific `IfcGuid`, `IfcPset.*`, and `IfcRel.*` markers must now map canonical IFC entity semantics and quantity units into the snapshot property bag before invoking the strict QA2 profile.

Do not synthesize fake entity names or units merely to clear the gate. If upstream data genuinely lacks them, keep the finding active or use the existing time-bounded audited waiver process when policy permits. This keeps Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate evidence explicit and reviewable.

## Regression coverage

`scripts/preflight-qa2-ifc-quantity-metadata.py` is auto-discovered by the repository feature-guard workflow and verifies the strict-profile registrations, missing/blank checks, hard-gate message path, and that the two completeness rules have not accidentally been promoted to structural/non-waivable identity rules.
