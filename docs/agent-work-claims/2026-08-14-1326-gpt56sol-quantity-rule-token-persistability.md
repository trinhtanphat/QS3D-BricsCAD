# Work claim — QuantityRule token persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-quantity-rule-token-persistability-20260814-1326`
- Registered: `2026-08-14T13:26:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `1d566ca246d9b1057ae33ae67e1e8327726a8a67`
- Claim commit: `9c9257d546d218ad9a5fbd0c4e8d7b88c2195d57`
- Pre-write source blob: `e76b2fbf9f6eb4068e7f1e676948142a479b1ce2`

## Confirmed defect

`QuantityRule` persists `Id`, `OutputName`, `Expression`, and `Version` as QSDB XML attributes. Its public constructor currently routes all four strings through the same `Required` helper, which rejects blank input and trims surrounding whitespace but accepts embedded control characters.

For the token-like fields `Id`, `OutputName`, and `Version`, supported construction can therefore create state that is not persistable when an XML-invalid control character such as `U+0001` is embedded. `OutputName` is additionally a quantity identity and is later passed to `ProjectElement.SetQuantity`, whose supported writer boundary now rejects control characters. Rule construction should fail at its own boundary rather than deferring failure to apply/save.

`Expression` is intentionally excluded from token validation because it is formula text, not an identity token; this claim does not redefine the expression grammar or whitespace contract.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — only `QuantityRule` constructor/string validation helpers.
- new `tests/QS3D.Core.SmokeTests/QuantityRuleTokenPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Introduce token-specific validation for `Id`, `OutputName`, and `Version`: preserve required-value rejection and surrounding-whitespace normalization, then reject control characters before field assignment. Keep `Expression` on the existing `Required` path unchanged. Preserve category validation, engine evaluation, ordering, provenance and variable-projection behavior.

## Regression plan

Focused self-registering smoke will prove:

1. canonical and padded token fields remain supported and normalize as before;
2. expression text remains on the existing required/trim path;
3. embedded `U+0001` is rejected independently for Id, OutputName, and Version at construction.

## Explicit non-scope

- no Expression grammar/control-character policy changes;
- no QuantityRuleEngine evaluation/dependency/provenance changes;
- no quantity variable projection or duplicate-rule logic changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but there is no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence; completion requires remote diff/readback and ancestry verification.
