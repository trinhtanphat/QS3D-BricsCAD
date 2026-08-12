# Work claim — Formula reference number-token parity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:15:00+07:00`
- Baseline main SHA: `5a9f54e256954e69ff1fc5ca0366ad46eaa29707`
- Priority: evidence-driven remote-safe formula dependency correctness

## Reason

`ExpressionEvaluator.GetReferencedVariables()` uses a lightweight numeric-token scanner before quantity-rule dependency scheduling, while `Evaluate()` uses the real parser. The scanner currently accepts incomplete exponent prefixes without validating the scanned token. For expression `1eFoo`, it skips `1e` and then reports `Foo` as a referenced variable. If `Foo` is also the managed output of the rule, `QuantityRuleEngine.ApplyMatching()` can therefore report a circular dependency instead of the actual malformed numeric literal. That is a deterministic parser/scanner contract mismatch and can mask invalid formula syntax.

## Reserved scope

Make numeric-token scanning in `GetReferencedVariables()` validate the complete scanned number with the same invariant floating-point rules used by `ParseNumber()`. Preserve expression grammar, dependency order, function handling, case-insensitive variable semantics, finite-result policy, depth/argument limits, rule application and public APIs. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` (`SkipNumberToken` parity only)
- `tests/QS3D.Core.SmokeTests/FormulaReferenceNumberTokenParitySmoke.cs`
- this claim file

## Excluded scope

- No formula grammar expansion or new functions/operators.
- No changes to rule output identity, circular-dependency semantics for valid formulas, variable normalization, persistence, UI/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Validation plan

- Assert `GetReferencedVariables("1eFoo")` fails as an invalid number rather than returning `Foo`.
- Assert `QuantityRuleEngine.ApplyMatching()` with managed output `Foo` and expression `1eFoo` reports invalid numeric syntax rather than a circular dependency and does not mutate the element quantity/provenance state.
- Assert valid exponent notation such as `1e2 + Foo` still reports only `Foo` and evaluates normally.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent formula claims for finite arithmetic, variable case/whitespace normalization and unary recursion depth are completed. No current/recent claim was found for numeric-token parity in reference extraction.

## Completion condition

Current `main` cannot misclassify malformed exponent syntax as a managed-output circular dependency, focused regression coverage is present, and this claim is marked `COMPLETED`.
