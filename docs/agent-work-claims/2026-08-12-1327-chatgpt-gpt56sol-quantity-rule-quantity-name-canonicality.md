# Work claim — Quantity Rule quantity-name canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-rule-quantity-name-canonicality`
- Registered: `2026-08-12T13:27:00+07:00`
- Baseline main SHA: `df848e474aeb308e2e10fa9343dc6d576f93cfc2`
- Priority: P1 — fail closed when a Quantity Rule projects malformed persisted quantity identities.

## Confirmed defect

`ProjectElement.Quantities` is publicly mutable. QSDB validation requires every quantity name to be nonblank and free of leading/trailing whitespace, but `QuantityRuleEngine.BuildVariables(...)` currently forwards quantity names through `AddVariable(...)`, which trims padded names and ignores blank names. A malformed quantity such as `" LengthM "` can therefore be consumed as canonical `LengthM` and drive rule output even though the same ProjectState cannot be saved as valid QSDB.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — quantity-map validation inside `BuildVariables(...)` only.
- one focused ModuleInitializer smoke under `tests/QS3D.Core.SmokeTests/`.
- this claim file.

## Exclusions

- Do not change `ExpressionEvaluator` caller-variable whitespace normalization.
- Do not change `AddNumeric(...)` behavior for Family/Element property metadata; the completed variable-projection lane intentionally ignores whitespace-only numeric metadata.
- Do not change QuantityRule persistence/schema, BOM/reporting, UI, BricsCAD runtime, or quantity value semantics.

## Intended contract

- Quantity Rule projection rejects blank or padded `element.Quantities` keys before evaluating or mutating outputs/provenance.
- Existing finite-value rejection remains unchanged.
- Canonical quantity names continue to project normally.

## Validation plan

Focused smoke proves padded and blank quantity names fail before rule output/provenance mutation and canonical quantity input still evaluates successfully. Source/test will be read back from current `main`; ancestry will be verified before closeout. No GitHub Actions or local BricsCAD qualification will be run.
