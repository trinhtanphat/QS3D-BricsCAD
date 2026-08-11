# Work claim — Quantity rule-set defined category lookup

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:52:00+07:00`
- Baseline main SHA: `937d64f14cc61bb51dac8371ed0c66ac7c26e243`
- Priority: evidence-driven remote-safe Core lookup integrity hardening

## Reason

`QuantityCalculationRuleSet` supports legacy quantity category codes through its integer lookup overloads. Its `ElementCategory` overloads currently cast any enum value to `int` without first checking that the enum is defined. An undefined cast such as `(ElementCategory)201` can therefore collide with a legitimate legacy compatibility rule code `201` and resolve a Room rule despite the caller having supplied an invalid native category value.

## Reserved scope

Fail closed when the `ElementCategory` lookup overloads receive undefined enum values, while preserving integer-code compatibility lookups, native-to-legacy fallback for the explicitly mapped valid categories, rule cloning, settings validation, and all valid lookup behavior. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityCalculationRuleSet.cs`
- `tests/QS3D.Core.SmokeTests/QuantityCalculationRuleSetDefinedCategorySmoke.cs`
- this claim file

## Excluded scope

- No changes to Quantity Settings UI/commands, diagnostics exporter/redaction, `QuantityRuleEngine`, formulas, deduction logic, reporting output, or BricsCAD V25 runtime.
- No removal/change of integer compatibility codes.
- No GitHub Actions dispatch.

## Validation plan

- Add an explicit legacy category rule code `201`; confirm integer lookup `TryGetCategoryRule(201, ...)` still succeeds.
- Assert `TryGetCategoryRule((ElementCategory)201, ...)` rejects the undefined native enum rather than colliding with compatibility code `201`.
- Add a legacy intersection involving code `201`; confirm integer lookup remains supported while the corresponding undefined-enum overload fails closed.
- Confirm a valid native `ElementCategory.Room` lookup can still fall back to code `201` when its native rule is absent.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The historical quantity-rule category constructor claim is `COMPLETED` and reserved `QuantityRuleEngine.cs`. Current Quantity activity is UI/diagnostics-oriented; no current claim was found for `QuantityCalculationRuleSet.cs` defined-enum lookup integrity.

## Completion condition

Current `main` keeps legacy integer rule-code compatibility but rejects undefined `ElementCategory` values at enum overloads, includes focused regression coverage, and this claim is marked `COMPLETED`.
