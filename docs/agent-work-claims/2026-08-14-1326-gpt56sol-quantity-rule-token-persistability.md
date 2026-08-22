# Work claim — QuantityRule token persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-quantity-rule-token-persistability-20260814-1326`
- Registered: `2026-08-14T13:26:00+07:00`
- Completed: `2026-08-14T13:29:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `1d566ca246d9b1057ae33ae67e1e8327726a8a67`
- Claim commit: `9c9257d546d218ad9a5fbd0c4e8d7b88c2195d57`
- Claim reconciliation: `784dc95fb1b85b6b51e4bd0b2b35029545b8a650`
- Pre-write source blob: `e76b2fbf9f6eb4068e7f1e676948142a479b1ce2`
- Source: `0416c56ca2ed500e3b45be3449d0004d78fa35a3`
- Regression: `28c37e92857a27351b85bcedd8441181dedba69c`

## Confirmed defect

`QuantityRule` persists `Id`, `OutputName`, `Expression`, and `Version` as QSDB XML attributes. Its public constructor routed all four strings through the same `Required` helper, which rejected blank input and trimmed surrounding whitespace but accepted embedded control characters.

For the token-like fields `Id`, `OutputName`, and `Version`, supported construction could therefore create state that is not persistable when an XML-invalid control character such as `U+0001` is embedded. `OutputName` is additionally a quantity identity and is later passed to `ProjectElement.SetQuantity`, whose supported writer boundary rejects control characters.

`Expression` remained deliberately outside token validation because it is formula text rather than an identity token; this lane did not redefine expression grammar or whitespace semantics.

## Completed change

- Added `RequiredToken`, which reuses existing required/trim normalization and then rejects control characters.
- Routed only `Id`, `OutputName`, and `Version` through `RequiredToken`.
- Kept `Expression = Required(expression, nameof(expression))` unchanged.
- Left category validation and all QuantityRuleEngine evaluation, dependency, ordering, provenance, cleanup and variable-projection logic unchanged.

## Regression coverage

Added self-registering `QuantityRuleTokenPersistabilitySmoke` which pins:

1. padded Id/OutputName/Version still normalize to canonical tokens;
2. expression text still follows the existing required/trim path;
3. embedded `U+0001` is rejected independently for Id;
4. embedded `U+0001` is rejected independently for OutputName;
5. embedded `U+0001` is rejected independently for Version.

## Validation

Remote GitHub source diff for `0416c56ca2ed500e3b45be3449d0004d78fa35a3` confirms only the three constructor callsites and `RequiredToken` helper changed. Remote regression diff for `28c37e92857a27351b85bcedd8441181dedba69c` confirms the focused cases use the existing `ElementCategory.ArchitecturalWall` enum and C# `\u0001` literals. GitHub compare reports the regression SHA is ahead of the source SHA with `0416c56ca2ed500e3b45be3449d0004d78fa35a3` as merge base; unrelated Curtain commits/merge do not alter this lane. Live `main` read at `78392fd8f105564f5fad55c98cff2424261227a3` has the regression SHA as its parent, confirming the lane remains on current lineage.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Explicit non-scope

- no Expression grammar/control-character policy changes;
- no QuantityRuleEngine evaluation/dependency/provenance changes;
- no quantity variable projection or duplicate-rule logic changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes.

## Completion condition

Satisfied: claim-first reservation, live baseline reconciliation, isolated token-boundary fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
