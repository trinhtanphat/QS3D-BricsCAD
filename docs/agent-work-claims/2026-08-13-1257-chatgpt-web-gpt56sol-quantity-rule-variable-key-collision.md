# Work claim — Quantity Rule normalized variable-key collision integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-key-collision-20260813-1257`
- Registered: `2026-08-13T12:57:00+07:00`
- Baseline main SHA: `2def3f9ec4b9e9e5c24cef57bbb4484832c4fdd5`
- Priority: `P0 Measurement Rules` — deterministic/fail-closed variable projection in the existing QuantityRuleEngine

## Confirmed gap

`QuantityRuleEngine.BuildVariables(...)` intentionally projects family properties first, then element properties, then persisted quantities so later scopes override earlier scopes after whitespace/case normalization. The existing `QuantityRuleVariableKeyCanonicalizationSmoke` locks that cross-scope precedence. `AddNumeric(...)` previously wrote normalized property names directly into one target dictionary, so two numeric keys inside the same property map such as `Factor` and ` Factor ` silently collapsed after trimming. Their winning value depended on source enumeration order instead of a unique persisted variable identity. This was inconsistent with `ExpressionEvaluator.NormalizeVariables(...)`, which rejects duplicate names after trimming/ignoring casing.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmoke.cs`
- this claim file.

## Implemented contract

- `AddNumeric(...)` now creates a per-source case-insensitive normalized-name set and rejects a second finite numeric property whose trimmed key collides within that same property collection.
- Family and element property collections are still projected by separate `AddNumeric(...)` calls, so the pre-existing cross-scope precedence remains unchanged: element numeric properties override family numeric properties, then persisted quantities override projected properties.
- No new rule/formula/provenance engine or expression-language behavior was introduced.
- Rejection happens while building the local variable dictionary, before staged rule output/provenance mutation.

## Commits

- Claim-only: `0782ddff0a4ee3b0a509ceecf98d3da9f36158e5` — `chore(agent): claim quantity rule variable key collision integrity`.
- Production fix: `8cbee91fc83b4c97c48b70f90486cd3f161b03be` — `fix(rules): reject ambiguous normalized property variables`.
- Regression: `909c687e1df3bbcf342b38c7d7c3d648772f6b1e` — `test(rules): cover normalized property key collisions`.

## Validation actually executed

- Re-fetched `main` immediately after claim publication and confirmed no overlapping Quantity Rules reservation appeared; concurrent commits were confined to MeasurementTrace/Curtain/native claim surfaces.
- Exact commit readback confirmed the production commit changes only `QuantityRuleEngine.cs` (+7/-1) and the regression commit changes only `QuantityRuleVariableKeyCanonicalizationSmoke.cs` (+35).
- Current-main readback after concurrent merges confirmed both reserved changes remain present.
- Existing `QuantityRuleVariableKeyCanonicalizationSmokeRegistration` uses `[ModuleInitializer]` to execute this smoke without any shared registration-file edit.
- Regression now covers both the retained cross-scope canonical precedence and same-map normalized numeric-key collision failure before quantity/provenance/freshness mutation.

## Unexecuted gates

- This connector environment did not execute `dotnet build`, the `QS3D.Core.SmokeTests` binary, GitHub Actions, or BricsCAD native/runtime qualification. No PASS is claimed for those unexecuted gates.

## Completion condition

Satisfied for this narrow P0 Measurement Rules integrity lane: ownership was claimed before source change, ambiguous same-map normalized numeric variables now fail closed in the existing engine, the intended cross-scope precedence remains locked by regression, exact source/test scope was read back from current `main`, and the claim is released as `COMPLETED` without representing unexecuted managed/native gates as PASS.
