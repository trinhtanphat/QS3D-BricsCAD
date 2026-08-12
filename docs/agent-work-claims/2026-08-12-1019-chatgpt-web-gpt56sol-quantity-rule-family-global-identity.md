# Work claim — QuantityRuleEngine global Family identity integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-family-global-identity-20260812-1019`
- Registered: `2026-08-12T10:19:00+07:00`
- Baseline main SHA: `2b6d5274ce07a9a4c93648ec31f73ba8ed7a8d0c`
- Priority: P1 — quantity-rule mutation must not run against an ambiguous Family identity space.
- Task Key: `CORE-QUANTITY-RULE-FAMILY-GLOBAL-IDENTITY`

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` globally validates quantity-rule IDs, but Family validation is limited to `ResolveFamily(project, element)`, which resolves only the target element's Family ID. A malformed project containing unrelated duplicate Families such as `F1` / `f1` plus a unique target Family `F2` can still evaluate rules for an `F2` element and write quantities/provenance. QSDB persistence and canonical Family mutation services reject that same Family collection as ambiguous.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyGlobalIdentitySmoke.cs`
- this claim file

## Intended contract

- Before Family resolution or quantity/provenance mutation, validate the complete project Family collection for null entries, blank/non-canonical IDs and case-insensitive duplicate IDs.
- Fail before element quantity/property/timestamp mutation when Family identity is ambiguous, even if the target element references a different unique Family.
- Preserve valid rule evaluation, rule-ID/output validation, dependency ordering, stale-output cleanup, family/category checks, provenance and no-rule cleanup semantics.
- Do not alter ProjectFamilyService, expression parsing, preview UI/native BricsCAD or persistence schema.

## Validation plan

Focused auto-registered Core smoke constructs unrelated `F1`/`f1` duplicates plus unique target `F2`, an `F2` element and a valid rule, then requires `ApplyMatching(...)` to reject without changing quantities, properties or element persistence state. A valid control proves rule evaluation with unique Families still writes the expected quantity/provenance. Re-fetch source/claim before writes. No force-push, GitHub Actions dispatch, executable full-smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
