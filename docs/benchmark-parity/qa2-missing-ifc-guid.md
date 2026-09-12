# QA Gate 2.0 — IFC GUID identity hard gate

## Purpose

The Solibri Quantity Strict QA profile now requires every model element admitted to guarded quantity workflows to carry a non-empty `IfcGuid` property.

Previously, QA Gate 2.0 detected duplicate IFC GUIDs only when a GUID value existed. An element with an absent or whitespace-only `IfcGuid` could therefore avoid both the missing-identity and duplicate-identity checks and reach Takeoff, BOQ, or Estimate. This diverged from the repository's older strict IFC quantity profile, which already treats `IfcGuid` as required identity evidence.

## Rule

`QA2.MISSING_IFC_GUID` is emitted when `IfcGuid` is missing or whitespace. `QsQaRuleProfile.SolibriQuantityStrict()` assigns the rule `Critical` severity.

Missing IFC identity is treated as a structural identity defect, alongside duplicate element IDs and duplicate IFC GUIDs. A matching waiver remains loadable and auditable as input, but it cannot move `QA2.MISSING_IFC_GUID` out of the active finding set. This prevents a waiver keyed only by local element ID from manufacturing an IFC identity that downstream quantity evidence does not actually have.

The existing `QA2.DUPLICATE_IFC_GUID` behavior is unchanged: GUID comparison remains trimmed and case-insensitive, and duplicate GUID conflicts remain non-waivable.

## Hard-gate behavior

When the configured blocking threshold includes `Critical`, a missing IFC GUID keeps the decision at `Blocked` and therefore keeps all three guarded workflows disabled:

- Takeoff
- BOQ
- Estimate

`DemandAllowed(...)` and `QsQaGuardedExecutor` continue to enforce the same fail-closed gate.

## Migration and compatibility

No public DTO, constructor, route, or rule-profile signature changes are introduced. Existing elements with valid `IfcGuid` values are unaffected.

IFC importers, adapters, and migration code that previously omitted GUIDs or populated them with whitespace must now map the canonical IFC GlobalId into `QsModelElementSnapshot.Properties["IfcGuid"]` before requesting strict QA approval. Do not synthesize a local placeholder solely to satisfy this rule; the GUID participates in duplicate-identity validation and downstream provenance.

Existing waiver records targeting `QA2.MISSING_IFC_GUID` can still be deserialized and inspected, but they cannot release the strict gate. Correct the source model identity instead.
