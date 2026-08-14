# Agent work claim — QuantityRule expression XML persistability

- Agent: `chatgpt-web-gpt56sol-quantity-rule-expression-xml-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `c07293988e67204ce31e3ff4bfc61d94a3611712`
- Claim commit: `b0197bcf43fdcafa256535647f6766a582641c04`
- Implementation branch: `agent/chatgpt-web-gpt56sol/quantity-rule-expression-xml-persistability-20260814`
- Source commit: `d12996bb5b0e6a1dec2dbc054ea7c9a96e7e761d`
- Regression commit / implementation head: `b54ae5a3e232c50edae8e5ae1ad8ad0c62252784`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-quantity-rule-expression-xml-persistability-20260814`
- Priority: Core P1 persistence integrity

## Reserved scope

Fix one confirmed constructor-to-QSDB persistability mismatch for `QuantityRule.Expression`. `Id`, `OutputName`, and `Version` already use token-specific control-character validation, but formula `Expression` intentionally remains on the generic required/trim path. That path admits XML-illegal characters such as `U+0001`, while QSDB persists `expression` directly as an XML attribute and serialized XML validation rejects such text.

This lane only requires formula text to be XML-representable after the existing required/trim normalization. It does not redefine expression grammar, operators, variable syntax, dependency semantics, or valid internal whitespace.

## Expected surfaces

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — `QuantityRule` constructor routes normalized expression text through XML-character validation while leaving token validation and engine logic unchanged.
- new `tests/QS3D.Core.SmokeTests/QuantityRuleExpressionPersistabilitySmoke.cs` — constructor rejection for XML-illegal expression text plus valid expression QSDB SaveNew→Load round-trip.
- this claim file for coordination/closeout evidence.

## Explicit non-scope

- No changes to `Id`, `OutputName`, `Version`, category validation, expression evaluator grammar, dependency resolution, provenance, variable projection, dirty propagation, rule ordering, family references, loader/schema/migration, UI/native adapters, release/CI/signing, or LOCAL_ONLY BricsCAD qualification.
- No blanket rejection of XML-valid tab/newline characters inside formula text; the boundary is XML representability, not identity-token policy.
- No manual GitHub Actions dispatch/rerun/cancel.

## Evidence before registration

At baseline `c07293988e67204ce31e3ff4bfc61d94a3611712`, the public `QuantityRule` constructor uses `RequiredToken(...)` for `Id`, `OutputName`, and `Version`, but `Expression = Required(expression, nameof(expression))`. The previous completed `QuantityRule token persistability` claim explicitly kept Expression unchanged and excluded expression grammar/control-character policy from that lane. QSDB serializes rule `expression` as an XML attribute, so an expression containing `U+0001` can be constructed but cannot be serialized as canonical QSDB XML.

No matching current commit/claim was found for QuantityRule expression XML persistability.

## Implementation evidence before integration

- Source commit `d12996bb5b0e6a1dec2dbc054ea7c9a96e7e761d` adds `RequiredXmlText`: it first applies the existing `Required(...)` trim/required semantics, then calls `XmlConvert.VerifyXmlChars`, converting XML-invalid formula text into `ArgumentException` at construction.
- Regression commit `b54ae5a3e232c50edae8e5ae1ad8ad0c62252784` adds `QuantityRuleExpressionPersistabilitySmoke`, pinning `U+0001` rejection, unchanged padded-expression trim behavior, and valid expression QSDB SaveNew→Load round-trip.
- Compare from claim commit to implementation head reports exactly two changed surfaces: `QuantityRuleEngine.cs` and the new focused smoke file.
- Source/test were read back; the regression uses the C# `\u0001` runtime escape. No managed/cloud/native PASS is claimed from the agent branch and no manual Actions dispatch was performed.

## Validation plan

- verify claim visibility on refreshed `main` before source work;
- make the smallest constructor/helper change using XML-character validation after existing required/trim normalization;
- add focused deterministic smoke source for invalid `U+0001` construction rejection and valid expression SaveNew→Load identity;
- read back diff, reconcile fresh `main`, land once through integration with `force:false`, observe automatic CI only, and report only evidence actually observed.

## Completion condition

Claim-first reservation, isolated source + regression, fresh-main integration/readback, and truthful CI/native boundaries are recorded; then status becomes `COMPLETED`.
