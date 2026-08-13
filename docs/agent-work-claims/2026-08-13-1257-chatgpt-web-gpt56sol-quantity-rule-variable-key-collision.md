# Work claim — Quantity Rule normalized variable-key collision integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-variable-key-collision-20260813-1257`
- Registered: `2026-08-13T12:57:00+07:00`
- Baseline main SHA: `2def3f9ec4b9e9e5c24cef57bbb4484832c4fdd5`
- Priority: `P0 Measurement Rules` — deterministic/fail-closed variable projection in the existing QuantityRuleEngine

## Confirmed gap

`QuantityRuleEngine.BuildVariables(...)` intentionally projects family properties first, then element properties, then persisted quantities so later scopes override earlier scopes after whitespace/case normalization. The existing `QuantityRuleVariableKeyCanonicalizationSmoke` locks that cross-scope precedence. However, `AddNumeric(...)` currently writes normalized property names directly into one target dictionary, so two numeric keys inside the same property map such as `Factor` and ` Factor ` silently collapse after trimming. Their winning value depends on source enumeration order instead of a unique persisted variable identity. This is inconsistent with `ExpressionEvaluator.NormalizeVariables(...)`, which rejects duplicate names after trimming/ignoring casing, and can make a Quantity Rule evaluate an ambiguous persisted state rather than fail closed.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs` — detect duplicate normalized numeric variable names within each individual family/element property map before projecting them; preserve existing family -> element -> quantity cross-scope precedence.
- `tests/QS3D.Core.SmokeTests/QuantityRuleVariableKeyCanonicalizationSmoke.cs` — add focused regression for same-map normalized-key collision plus preservation of existing cross-scope precedence.
- this claim file.

## Contract boundary

- Do not introduce a second rule, formula, quantity, provenance, or dependency engine.
- Existing `ExpressionEvaluator`, rule dependency ordering, provenance, stale-output cleanup, and quantity-name canonicality remain authoritative and unchanged.
- Cross-scope collision is intentional precedence and remains allowed: element numeric properties override family numeric properties; persisted quantities override projected properties.
- Only two distinct numeric keys from the same property collection that normalize to the same variable name (trimmed + case-insensitive) are rejected before rule output/provenance mutation.

## Excluded scope

- No `MeasurementTrace` / MTR source or smoke files; active nullable-integrity ownership remains untouched.
- No Takeoff/MTR-03, palette, Curtain/native, LOCAL-003, persistence/schema, report/BOQ/estimate, BricsCAD adapter/runtime, installer, CI or GitHub Actions changes.
- No change to public `QuantityRule` shape or expression language.

## Validation plan

- Regression constructs one element property map containing two numeric keys that normalize to the same variable name and proves `ApplyMatching(...)` throws before writing output/provenance or freshness state.
- Existing canonicalization regression continues to prove family padded `Factor` -> element `factor` -> quantity `LengthM` precedence evaluates unchanged.
- Re-fetch current `main`, re-read all ACTIVE/BLOCKED claims immediately after this claim lands, and abort/source-skip if any overlapping reservation appeared.
- Re-fetch implementation/test from final `main` and inspect exact diff/scope after push.
- Connector-only validation is not an executable .NET build/smoke run; no build/native PASS will be claimed unless actually executed.
