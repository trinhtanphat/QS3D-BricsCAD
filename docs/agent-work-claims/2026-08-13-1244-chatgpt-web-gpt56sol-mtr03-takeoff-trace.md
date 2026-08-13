# Work claim — MTR-03 raw Takeoff QuantityEngine trace projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr03-takeoff-trace-20260813-1244`
- Registered: `2026-08-13T12:44:00+07:00`
- Baseline main SHA: `50931798b3e74a2e78497040364058df06ecf124`
- Priority: `MTR-03 / P0` — project one existing canonical quantity path into MeasurementTrace

## Confirmed gap

`QS3D.Core.Takeoff.QuantityEngine.Calculate(...)` is the canonical raw entity count/length/area/volume conversion path and currently returns only `TakeoffResult`. MTR-01 now provides the canonical `MeasurementTrace` contract, while source/history inspection found no MTR-03 trace projection for this path. Existing `UnitScale` conversion remains authoritative and must not be copied into a second formula path.

## Reserved scope

- `src/QS3D.Core/Takeoff/QuantityEngine.cs` — add a trace-returning projection that invokes the existing canonical `Calculate(...)` path exactly once and constructs trace facts from the same input snapshot/result;
- new `src/QS3D.Core/Takeoff/TakeoffResultWithTrace.cs` — immutable pairing of canonical `TakeoffResult` and `MeasurementTrace` only;
- new `tests/QS3D.Core.SmokeTests/TakeoffMeasurementTraceSmoke.cs` — focused CAD-independent parity/provenance regression;
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — one registration call only; this is a shared registration surface and no other smoke semantics are reserved;
- this claim file.

## Contract boundary

- `QuantityEngine.Calculate(...)` remains the canonical calculation/conversion API and behavior.
- Trace projection must call that canonical API and use its returned value/unit; it may inspect raw `EntitySnapshot` facts for explanation but must not independently convert or recompute the output.
- For raw source takeoff there is no semantic project element yet, so the source entity Handle is the trace semantic/source identity for this stage.
- Count has no metric conversion; length/area/volume trace records the raw drawing-unit fact and the explicit drawing-unit conversion assumption while gross/net equal the authoritative `TakeoffResult.Value` and adjustments are empty.
- No rounding is introduced; rounding policy remains explicit as `none`.

## Excluded scope

- No `UnitScale` changes or duplicated unit formula.
- No semantic regenerators, Quantity Rules, project/report/UI, persistence/schema, snapshots/deltas, BOQ/estimate, BricsCAD adapter/native runtime, wall takeoff workspace or command changes.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.
- `LOCAL-003` Level/native surfaces remain fully excluded.

## Validation plan

- Regression proves `CalculateWithTrace(...).Result` exactly matches `Calculate(...)` for count/length/area/volume and trace gross/net/unit equal that canonical result.
- Regression proves raw input fact + drawing-unit provenance are captured without changing conversion behavior, and invalid/missing metrics continue to fail through the existing canonical calculation path.
- Re-fetch/read back implementation files and exact diff from current `main` after source push.
- Connector-only readback is not an executable .NET smoke/build run; no such PASS will be claimed unless actually executed.
