# Work claim — Quantity Rule duplicate-id integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-duplicate-id-integrity-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Baseline main SHA: `f9f6332bde8bc958cdeda748b586a59b15ae8b5e`
- Priority: P1 persisted identity / rule provenance integrity found during owner-requested full review/fix continuation

## Confirmed defect

`ProjectState.FindQuantityRule(...)` defines duplicate Quantity Rule IDs as invalid project state, but `QuantityRuleEngine.ApplyMatching(...)` currently validates only duplicate output names. Two persisted rules can therefore share the same `Id` while targeting different outputs; both are evaluated and publish provenance using the indistinguishable `RuleId@Version` token. This bypasses the canonical rule-identity invariant and makes generated quantity provenance ambiguous.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — rule identity preflight in `ApplyMatching(...)` only.
- one focused CAD-independent Core smoke fixture/registration.
- this claim file.

## Contract

Fail closed on duplicate persisted Quantity Rule IDs, case-insensitively, before category filtering, stale managed-output cleanup, expression evaluation, provenance writes, or semantic mutation. Preserve the existing null-rule guard, duplicate-output validation, dependency ordering, stale-output behavior and valid provenance semantics.

## Validation plan

Add deterministic smoke coverage proving two rules with the same ID but different outputs are rejected with `InvalidOperationException` while preexisting quantity/provenance/freshness remain unchanged; also prove distinct IDs continue to evaluate and publish distinct canonical provenance. Re-fetch moving `main` before every write; never force-push. No GitHub Actions dispatch, executable test PASS, or BricsCAD runtime qualification claim.
