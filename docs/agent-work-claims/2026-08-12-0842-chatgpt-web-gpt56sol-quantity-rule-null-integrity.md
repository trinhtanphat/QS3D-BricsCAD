# Work claim — Quantity Rule null-collection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-null-integrity-20260812-0842`
- Registered: `2026-08-12T08:42:00+07:00`
- Completed: `2026-08-12T08:48:00+07:00`
- Baseline main SHA: `2ecb42affc613707e5b25d1760411738be8d6701`
- Claim commit: `bab5a1b3eecf050f93cdf92436f7d0cf67e07a9c`
- Implementation commit: `1494fe2bcc72218198c6e878a42e8057d34cc612`
- Regression-test commit: `89171d12f09bdf9a164cd663e637c9ee1e5b2b7a`
- Final pushed product/test SHA: `89171d12f09bdf9a164cd663e637c9ee1e5b2b7a`
- Priority: evidence-driven Core rule evaluation integrity during owner-requested full review/fix continuation

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` enumerated `project.QuantityRules` with `.Where(x => x.Category == element.Category)` before validating persisted rule entries. A malformed project containing a null Quantity Rule could therefore throw an incidental `NullReferenceException`. `ProjectState.FindQuantityRule(...)` already treats a null rule entry as invalid project state, so rule evaluation must fail closed before stale-output cleanup or quantity mutation.

## Implemented

`ApplyMatching(...)` now rejects any null persisted Quantity Rule entry with the canonical `InvalidOperationException` immediately after canonical project-element ownership validation and before rule filtering, output validation, stale managed-output discovery/cleanup, expression staging, or semantic mutation.

## Regression coverage

`QuantityRuleNullPreflightSmoke` now pins both sides of the contract:

- malformed null-rule state fails with the canonical integrity error while preserving stale managed quantity, provenance, and element freshness;
- a valid matching rule still evaluates, publishes its output, reports one applied rule, and writes canonical rule provenance.

## Validation boundary

The implementation and smoke source were re-read from current `main` after their writes. No GitHub Actions workflow was dispatched or re-run. No executable Core build/smoke PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this web session.

## Outcome

Quantity Rule evaluation now fails closed on malformed persisted rule collections before any managed-output cleanup or quantity mutation, while preserving normal rule evaluation semantics.
