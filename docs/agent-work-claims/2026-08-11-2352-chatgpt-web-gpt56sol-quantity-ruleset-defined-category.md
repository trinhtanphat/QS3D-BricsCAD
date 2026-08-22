# Work claim — Quantity rule-set defined category lookup

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:52:00+07:00`
- Baseline main SHA: `937d64f14cc61bb51dac8371ed0c66ac7c26e243`
- Priority: evidence-driven remote-safe Core lookup integrity hardening

## Reason

`QuantityCalculationRuleSet` supports legacy quantity category codes through its integer lookup overloads. Its `ElementCategory` overloads cast enum values to `int` without first checking that the enum was defined. An undefined cast such as `(ElementCategory)201` could therefore collide with a legitimate legacy compatibility rule code `201` and resolve a Room rule despite the caller having supplied an invalid native category value.

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

The historical quantity-rule category constructor claim is `COMPLETED` and reserved `QuantityRuleEngine.cs`. Current Quantity activity remained UI/diagnostics-oriented; no current claim was found for `QuantityCalculationRuleSet.cs` defined-enum lookup integrity. Normal concurrent-main `409` races occurred during one source attempt and the first close attempt; each time the current blob was re-fetched and no force update was used.

## Completion

- Implementation commits:
  - `fac8123ad8ff5235b97bba3eb26b75e2de56c127` — validate `ElementCategory` inside the shared enum-to-code lookup path before native/compatibility resolution.
  - `299705ab74a8b274a44981847168b538968e7245` — add legacy category/intersection collision regression coverage and preserve valid Room fallback/integer-code behavior.
- Final observed `main` before claim close: `c98dfcf5813ccc3ed53bb2a8f999080a8459170f`.
- Validation actually performed:
  - re-fetched `QuantityCalculationRuleSet.cs` from current `main` and confirmed the defined-enum guard is in `LookupCodes`, covering both enum overloads while leaving integer overloads unchanged;
  - re-fetched the dedicated smoke and confirmed integer code `201` remains supported, valid `ElementCategory.Room` still falls back to `201`, and undefined `(ElementCategory)201` fails closed for category and intersection lookups;
  - did not modify Quantity Settings UI/commands, `QuantityRuleEngine`, diagnostics, formulas or deduction logic;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core rule-set lookup integrity hardening.

## Completion condition

Satisfied: current `main` keeps legacy integer rule-code compatibility but rejects undefined `ElementCategory` values at enum overloads, includes focused regression coverage, and this claim is released as `COMPLETED`.
