# Work claim — MTR-05 `none` rounding trace reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260813-1827`
- Registered: `2026-08-13T18:27:34+07:00`
- Baseline main SHA: `7cf118157e8ca7189fad0400428ee9c92ee77e27`
- Priority: P0 / MTR-05 continuous hardening. Current `MeasurementTrace` validates finite values, units, duplicate evidence and rule-pair integrity, but a trace declaring `roundingPolicy = "none"` can still carry gross/adjustment/net values that do not reconcile, making canonical explainability self-contradictory.

## Reserved scope

Fail closed when a `MeasurementTrace` explicitly declares `roundingPolicy = "none"` but its canonical gross value plus additions minus deductions does not equal the canonical net value. Keep the check inside the canonical MeasurementTrace contract; do not duplicate or change category quantity formulas.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file for close-out

## Excluded scope

- any rounding policy other than the exact canonical token `none`;
- category-specific quantity math, Wall/Raw Takeoff projections, report/UI inspector logic, snapshots/deltas, cost, mapping, persistence or native BricsCAD code;
- current ACTIVE/BLOCKED Platform/CAD sibling documentation, SE closed-polyline native workflow, startup/runtime, responsive UI and other feature-specific lanes;
- GitHub Actions, packaging, release or native V25/V26 qualification.

## Validation plan

- add focused smoke regression proving contradictory `none` traces fail closed while valid deduction/addition traces continue to construct;
- preserve existing canonical MTR1/MTR2 bytes for valid traces;
- run the smallest locally available managed/static check only if the toolchain is available; otherwise record remote source/test readback without claiming runtime PASS;
- refresh current `main` after this claim lands, verify this claim commit remains on lineage, and recheck overlap before source mutation.

## Coordination

MTR-05 duplicate trace evidence was already claimed, fixed, regressed and completed on current lineage (`59f44b2` → `b98410a` → `e9de034` → `c778b43`), so this claim does not duplicate it. The completed MTR-03 Wall trace projection uses canonical `roundingPolicy = "none"` and exact canonical quantity outputs; this lane hardens only the shared trace invariant and does not own Wall formulas or projection behavior.

## Completion condition

Current `main` rejects self-contradictory `roundingPolicy = "none"` traces, focused regression coverage is pushed, remote source/test readback is verified, and this claim is marked `COMPLETED` with exact implementation/validation evidence. No native PASS is claimed unless actually executed.