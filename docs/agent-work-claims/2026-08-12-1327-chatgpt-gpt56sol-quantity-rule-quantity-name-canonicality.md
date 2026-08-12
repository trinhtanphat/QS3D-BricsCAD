# Work claim — Quantity Rule quantity-name canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-rule-quantity-name-canonicality`
- Registered: `2026-08-12T13:27:00+07:00`
- Baseline main SHA: `df848e474aeb308e2e10fa9343dc6d576f93cfc2`
- Priority: P1 — fail closed when a Quantity Rule projects malformed persisted quantity identities.

## Confirmed defect

`ProjectElement.Quantities` is publicly mutable. QSDB validation requires every quantity name to be nonblank and free of leading/trailing whitespace, but `QuantityRuleEngine.BuildVariables(...)` previously forwarded quantity names through `AddVariable(...)`, which trims padded names and ignores blank names. A malformed quantity such as `" LengthM "` could therefore be consumed as canonical `LengthM` and drive rule output even though the same ProjectState cannot be saved as valid QSDB.

## Completed scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — quantity-map validation inside `BuildVariables(...)` only.
- `tests/QS3D.Core.SmokeTests/QuantityRuleQuantityNameCanonicalitySmoke.cs`.
- this claim file.

## Exclusions preserved

- `ExpressionEvaluator` caller-variable whitespace normalization is unchanged.
- `AddNumeric(...)` behavior for Family/Element property metadata is unchanged; the completed variable-projection lane still intentionally ignores whitespace-only numeric metadata.
- QuantityRule persistence/schema, BOM/reporting, UI, BricsCAD runtime, and quantity value semantics are unchanged.

## Product commits

- Claim: `3cb9346afc5123fcbff9090c145e2c329ff82a1c`.
- Source fix: `52edfd77d9127c6abc0a7df4956f12c42c7b0858`.
- Focused smoke: `1aeeb42b6f02e4c53b47b0530f4799f8beeba618`.

## Final contract and validation

- Quantity Rule projection now rejects blank or padded `element.Quantities` keys before formula evaluation/output/provenance mutation.
- Existing finite-value rejection remains unchanged.
- Canonical quantity names continue to project normally; the focused smoke verifies `LengthM=3` evaluates `Result=6` with canonical `Rule:Result` provenance.
- Focused smoke also verifies padded and blank quantity keys fail without changing output count, provenance, `UpdatedUtc`, or dirty state.
- Source and smoke were read back from current `main` after integration.
- Compare of `1aeeb42b6f02e4c53b47b0530f4799f8beeba618...main` reported the regression as an ancestor (`ahead_by: 3`, `behind_by: 0`) during closeout preparation.
- No GitHub Actions were dispatched. No local .NET build/Core smoke execution or BricsCAD V25/V26 runtime PASS is claimed.

## Completion

Remote source-safe scope is complete.
