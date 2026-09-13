# QA Gate 2.0 classification completeness

## Contract

`QsQaRuleProfile.SolibriQuantityStrict()` now requires every quantity-bearing model snapshot to carry a non-empty canonical `Classification`. Missing or whitespace-only classification produces `QA2.MISSING_CLASSIFICATION` with `Error` severity.

Because the strict profile blocking threshold is `Error`, the finding blocks Takeoff, BOQ, and Estimate through the existing QA Gate 2.0 hard gate.

## Compatibility

No public constructor, DTO, workflow enum, waiver DTO, rule-profile constructor, or guarded-executor API changed. Existing classified models behave exactly as before.

The rule remains configurable: callers that construct a custom `QsQaRuleProfile` can override `QA2.MISSING_CLASSIFICATION` severity, and the normal audited waiver mechanism applies because missing classification is a completeness defect rather than an ambiguous structural-identity defect.

## Migration

Adapters that previously emitted an empty classification must map the authoritative project/IFC/classification-system code into `QsModelElementSnapshot.Classification` before invoking QA2. Do not synthesize a placeholder merely to release the gate; the value should remain traceable to the source classification used by quantity grouping and downstream commercial workflows.

This closes a parity gap with the legacy `QsQaProfile.StrictIfcQuantity()` contract, which already required classification completeness.
