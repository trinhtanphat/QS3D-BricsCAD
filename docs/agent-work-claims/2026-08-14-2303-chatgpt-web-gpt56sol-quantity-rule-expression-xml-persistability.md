# Agent work claim — QuantityRule expression XML persistability

- Agent: `chatgpt-web-gpt56sol-quantity-rule-expression-xml-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `c07293988e67204ce31e3ff4bfc61d94a3611712`
- Claim commit: `b0197bcf43fdcafa256535647f6766a582641c04`
- Implementation branch: `agent/chatgpt-web-gpt56sol/quantity-rule-expression-xml-persistability-20260814`
- Source commit: `d12996bb5b0e6a1dec2dbc054ea7c9a96e7e761d`
- Regression commit / implementation head: `b54ae5a3e232c50edae8e5ae1ad8ad0c62252784`
- Integration branch: `integration/chatgpt-web-gpt56sol-quantity-rule-expression-xml-persistability-20260814`
- Final integration / source landing: `fd74a3a030f03ad1c3192006c3eb77e2584c1775`
- Priority: Core P1 persistence integrity

## Reserved scope

Fixed one confirmed constructor-to-QSDB persistability mismatch for `QuantityRule.Expression`. `Id`, `OutputName`, and `Version` already use token-specific control-character validation, but formula `Expression` intentionally remained on the generic required/trim path. That path admitted XML-illegal characters such as `U+0001`, while QSDB persists `expression` directly as an XML attribute and serialized XML validation rejects such text.

This lane only requires formula text to be XML-representable after the existing required/trim normalization. It does not redefine expression grammar, operators, variable syntax, dependency semantics, or valid internal whitespace.

## Changed surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — `QuantityRule.Expression` now uses `RequiredXmlText`, which first applies the existing required/trim normalization and then `XmlConvert.VerifyXmlChars`; XML-invalid text fails at construction as `ArgumentException`.
- `tests/QS3D.Core.SmokeTests/QuantityRuleExpressionPersistabilitySmoke.cs` — focused deterministic source coverage for `U+0001` rejection, unchanged padded-expression trim behavior, and valid expression QSDB SaveNew→Load round-trip.

## Explicit non-scope preserved

- `Id`, `OutputName`, `Version`, category validation, expression evaluator grammar, dependency resolution, provenance, variable projection, dirty propagation, rule ordering, family references, loader/schema/migration, UI/native adapters, release/CI/signing were not changed.
- XML-valid formula whitespace is not blanket-rejected; the added boundary is XML representability, not identity-token policy.
- No manual GitHub Actions dispatch/rerun/cancel was performed.
- Licensed/native BricsCAD V25/V26 qualification remains LOCAL_ONLY.

## Evidence and integration

- At baseline `c07293988e67204ce31e3ff4bfc61d94a3611712`, `QuantityRule` used `RequiredToken(...)` for `Id`, `OutputName`, and `Version`, but `Expression = Required(expression, nameof(expression))`. The completed earlier `QuantityRule token persistability` claim explicitly kept Expression unchanged and excluded expression control-character/grammar policy, so this lane did not duplicate that scope.
- Claim-only reservation landed on `main` at `b0197bcf43fdcafa256535647f6766a582641c04` before source work.
- Source commit `d12996bb5b0e6a1dec2dbc054ea7c9a96e7e761d` and regression head `b54ae5a3e232c50edae8e5ae1ad8ad0c62252784` were read back from the agent branch. Compare from claim to implementation head reported exactly two changed surfaces: `QuantityRuleEngine.cs` and the new focused smoke file.
- Regression readback confirms the C# `\u0001` runtime escape, exact preservation of the existing trim contract for a valid padded expression, and QSDB round-trip assertions.
- Implementation SHAs were recorded on `main` at `5f71e85b85e63adcb9eabb70b2d4fe61aad09f08`. Integration candidate `fd74a3a030f03ad1c3192006c3eb77e2584c1775` was built from that refreshed main with implementation head `b54ae5a3e232c50edae8e5ae1ad8ad0c62252784` as additional parent.
- Freeze compare from `5f71e85b85e63adcb9eabb70b2d4fe61aad09f08` to the candidate reported exactly the two reserved files. Final refresh still showed main at that parent; `main` was then fast-forwarded to `fd74a3a030f03ad1c3192006c3eb77e2584c1775` with `force:false`, and immediate readback confirmed that exact source landing SHA.
- The standing automatic post-integration dispatcher created run `31817772628` for exact source SHA `fd74a3a030f03ad1c3192006c3eb77e2584c1775`; it was `in_progress` at close. Therefore this claim does not report managed/cloud CI PASS.
- No native BricsCAD runtime validation was executed by this remote lane and no native PASS is claimed.

## Completion

The QuantityRule expression XML-persistability fix and focused regression source are reachable from `main` at `fd74a3a030f03ad1c3192006c3eb77e2584c1775`. Claim-first/source/integration protocol is complete, the earlier token lane remains intact, no force push/manual CI dispatch was used, and validation limitations are explicit.
