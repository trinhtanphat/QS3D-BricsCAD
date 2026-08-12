# Work claim — Quantity Rule null-collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-null-integrity-20260812-0842`
- Registered: `2026-08-12T08:42:00+07:00`
- Baseline main SHA: `2ecb42affc613707e5b25d1760411738be8d6701`
- Priority: evidence-driven Core rule evaluation integrity during owner-requested full review/fix continuation

## Confirmed defect

`QuantityRuleEngine.ApplyMatching(...)` enumerates `project.QuantityRules` with `.Where(x => x.Category == element.Category)` before validating persisted rule entries. A malformed project containing a null Quantity Rule can therefore throw an incidental `NullReferenceException`. `ProjectState.FindQuantityRule(...)` already treats a null rule entry as invalid project state, so rule evaluation should fail closed with the same integrity semantics before any stale-output cleanup or quantity mutation.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — `ApplyMatching(...)` persisted rule preflight only.
- one focused CAD-independent Core smoke fixture/registration.
- this claim file.

## Contract

Reject null persisted Quantity Rule entries before rule filtering, expression staging, stale managed-output cleanup, or semantic mutation. Preserve canonical ownership checks, valid rule dependency ordering, stale-output behavior, formula evaluation, and provenance semantics.

## Validation plan

Add deterministic smoke coverage proving null rule state throws `InvalidOperationException` and leaves element quantities/properties/freshness unchanged, while valid matching rules still evaluate normally. Re-fetch current source before every write; never force-push. No GitHub Actions dispatch, executable test PASS, or BricsCAD runtime qualification claim.
