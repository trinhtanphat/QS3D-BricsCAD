# Work claim — Takeoff result integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:46:00+07:00`
- Baseline main SHA: `580079fc7832138186be362314fc85a7faad50de`
- Priority: evidence-driven remote-safe Core DTO hardening

## Reason

`TakeoffResult` is publicly constructible but currently accepts blank handles/units, undefined `TakeoffKind`, negative values, `NaN`, and infinities. `QuantityEngine` itself never emits those states, so direct public construction can create result objects outside the established takeoff contract and pass malformed quantities downstream.

## Reserved scope

Validate the public `TakeoffResult` constructor so handle/unit are required, kind is a defined enum value, and value is finite/non-negative. Preserve `QuantityEngine`, unit conversion, valid zero values, exact engine-generated units/values, and public property types. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Takeoff/TakeoffResult.cs`
- `tests/QS3D.Core.SmokeTests/TakeoffResultIntegritySmoke.cs`
- this claim file

## Excluded scope

- No changes to quantity conversion, `QuantityEngine`, wall quantity/reporting, XLSX export, drawing units, UI, or BricsCAD V25 runtime.
- No change to valid takeoff result values.
- No GitHub Actions dispatch.

## Validation plan

- Assert blank handle/unit, undefined kind, negative value, `NaN`, and infinities fail construction.
- Assert zero and normal positive values remain valid.
- Assert a result produced by `QuantityEngine.Calculate()` retains its expected value/unit contract.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Historical wall-takeoff claims are completed and do not reserve the generic `TakeoffResult` DTO. No current takeoff-result integrity claim was found.

## Completion condition

Current `main` rejects malformed public takeoff result state without changing engine-generated valid results, includes focused regression coverage, and this claim is marked `COMPLETED`.
