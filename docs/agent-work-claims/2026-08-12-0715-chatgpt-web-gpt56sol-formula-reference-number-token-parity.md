# Work claim — Formula reference number-token parity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:15:00+07:00`
- Completed: `2026-08-12T07:18:00+07:00`
- Baseline main SHA: `5a9f54e256954e69ff1fc5ca0366ad46eaa29707`
- Priority: evidence-driven remote-safe formula dependency correctness

## Reason

`ExpressionEvaluator.GetReferencedVariables()` used a lightweight numeric-token scanner before quantity-rule dependency scheduling, while `Evaluate()` used the real parser. The scanner accepted incomplete exponent prefixes without validating the scanned token. For expression `1eFoo`, it skipped `1e` and then reported `Foo` as a referenced variable. If `Foo` was also the managed output of the rule, `QuantityRuleEngine.ApplyMatching()` could therefore report a circular dependency instead of the actual malformed numeric literal. That was a deterministic parser/scanner contract mismatch that masked invalid formula syntax.

## Reserved scope

Make numeric-token scanning in `GetReferencedVariables()` validate the complete scanned number with the same invariant floating-point rules used by `ParseNumber()`. Preserve expression grammar, dependency order, function handling, case-insensitive variable semantics, finite-result policy, depth/argument limits, rule application and public APIs. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` (`GetReferencedVariables` number-token validation only)
- `tests/QS3D.Core.SmokeTests/FormulaReferenceNumberTokenParitySmoke.cs`
- this claim file

## Excluded scope

- No formula grammar expansion or new functions/operators.
- No changes to rule output identity, circular-dependency semantics for valid formulas, variable normalization, persistence, UI/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Completion

- Claim commit: `ac3090219050c8c67220e5443447647e0ec20c59`.
- Implementation commit: `84e358cc05cb18ee0a66fdcd90f28c35ab65b54f` — validate the token scanned by `GetReferencedVariables()` with invariant finite `double.TryParse` parity before continuing dependency extraction.
- Regression commit: `255d8b6d3fc5d3c0d614f949b76117621f3a8f89` — cover direct malformed exponent rejection, quantity-rule scheduler error identity/no mutation, and valid exponent extraction/evaluation.
- Final observed `main` before close: `255d8b6d3fc5d3c0d614f949b76117621f3a8f89`.
- Validation actually performed:
  - re-fetched current `ExpressionEvaluator` and confirmed number tokens are validated immediately after reference scanning;
  - re-fetched the dedicated smoke source and confirmed integration asserts malformed `1eFoo` is not reported as a circular dependency and leaves quantity/provenance state unchanged;
  - confirmed valid `1e2 + Foo` remains accepted by the same invariant parsing rules;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Recent formula claims for finite arithmetic, variable case/whitespace normalization and unary recursion depth were completed. No current/recent claim was found for numeric-token parity in reference extraction.

## Completion condition

Satisfied: current `main` cannot misclassify malformed exponent syntax as a managed-output circular dependency, focused regression coverage is present, and this claim is released as `COMPLETED`.
