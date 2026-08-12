# Work claim — QuantityRuleEngine global Family identity integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-family-global-identity-20260812-1019`
- Registered: `2026-08-12T10:19:00+07:00`
- Last Updated: `2026-08-12T10:22:00+07:00`
- Baseline main SHA: `2b6d5274ce07a9a4c93648ec31f73ba8ed7a8d0c`
- Source fix SHA: `a2986e5e708ee78f69ffb63a949b829fb492c893`
- Regression SHA: `9e063e605e17c98ff11b04dc0422f2c4892653ee`
- Priority: P1 — quantity-rule mutation must not run against an ambiguous Family identity space.
- Task Key: `CORE-QUANTITY-RULE-FAMILY-GLOBAL-IDENTITY`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` globally validated quantity-rule IDs, but Family validation was limited to `ResolveFamily(project, element)`, which resolved only the target element's Family ID. A malformed project containing unrelated duplicate Families such as `F1` / `f1` plus a unique target Family `F2` could still evaluate rules for an `F2` element and write quantities/provenance even though QSDB persistence and canonical Family services reject the same Family collection as ambiguous.

## Completed implementation

- `ApplyMatching(...)` now validates the complete project Family collection before Family resolution or rule evaluation.
- Null Family entries, blank/non-canonical Family IDs and case-insensitive duplicate Family IDs fail closed before quantity/provenance mutation.
- Existing rule-ID/output validation, dependency ordering, stale-output cleanup, family/category checks, provenance generation and valid rule evaluation remain unchanged.
- No expression-parser, preview UI/native BricsCAD or persistence-schema behavior was changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/QuantityRuleFamilyGlobalIdentitySmoke.cs` is auto-registered and covers:

- unrelated `F1` / `f1` duplicates plus unique target `F2` are rejected;
- rejection preserves quantities, properties/provenance, dirty state and element UpdatedUtc;
- a valid unique-Family control still applies one rule, writes the expected `RuleQ=2` quantity and canonical `Rule:RuleQ=R1@1` provenance.

Source and regression were read back directly from `main` after their commits.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: quantity-rule matching now fails closed on globally ambiguous Family identity before semantic quantity/provenance mutation while preserving valid rule behavior.
