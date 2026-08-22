# Work claim — MTR-03 raw Takeoff QuantityEngine trace projection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr03-takeoff-trace-20260813-1244`
- Registered: `2026-08-13T12:44:00+07:00`
- Baseline main SHA: `50931798b3e74a2e78497040364058df06ecf124`
- Priority: `MTR-03 / P0` — project one existing canonical quantity path into MeasurementTrace

## Confirmed gap

`QS3D.Core.Takeoff.QuantityEngine.Calculate(...)` was the canonical raw entity count/length/area/volume conversion path and returned only `TakeoffResult`. MTR-01 supplied the canonical `MeasurementTrace` contract, while source/history inspection found no trace projection for this path. Existing `UnitScale` conversion remains authoritative and is not copied into a second formula path.

## Reserved scope

- `src/QS3D.Core/Takeoff/QuantityEngine.cs` — trace-returning projection that invokes the existing canonical `Calculate(...)` path exactly once and constructs trace facts from the same input snapshot/result;
- new `src/QS3D.Core/Takeoff/TakeoffResultWithTrace.cs` — immutable pairing of canonical `TakeoffResult` and `MeasurementTrace` only;
- new `tests/QS3D.Core.SmokeTests/TakeoffMeasurementTraceSmoke.cs` — focused CAD-independent parity/provenance regression;
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — one registration call only;
- this claim file.

## Contract boundary

- `QuantityEngine.Calculate(...)` remains the canonical calculation/conversion API and behavior.
- `CalculateWithTrace(...)` calls that canonical API first and uses its returned value/unit. It does not call `UnitScale` or reproduce conversion factors.
- For raw source takeoff there is no semantic project element yet, so the source entity Handle is the trace semantic/source identity for this stage.
- Count has no metric conversion. Length/area/volume traces record one raw drawing-unit fact and an explicit drawing-unit conversion-path assumption; gross/net equal the authoritative `TakeoffResult.Value` and adjustments are empty.
- No rounding is introduced; rounding policy is explicit as `none`.

## Excluded scope

- No `UnitScale` changes or duplicated unit formula.
- No semantic regenerators, Quantity Rules, project/report/UI, persistence/schema, snapshots/deltas, BOQ/estimate, BricsCAD adapter/native runtime, wall takeoff workspace or command changes.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.
- `LOCAL-003` Level/native surfaces remained fully excluded; its concurrent claim moved to `BLOCKED` during this lane without touching the reserved MTR surfaces.
- The later `MTR foundation nullable contract compile integrity` claim owns only `MeasurementTrace.cs` + `MeasurementTraceContractSmoke.cs`; this lane did not edit either file.

## Completion

- Claim-only commit on `main`: `e2ef91fccd3019ccc6dfeaa4dc14b905bec859ad` — `chore(agent): claim MTR-03 raw takeoff trace projection`.
- Implementation + regression commit on `main`: `6b0c522a036891573610a5cc96764ed849aa9900` — `feat(takeoff): project canonical results into measurement traces`.
- Nullable compile-integrity follow-up on this lane's smoke: `ea3f4a7e55558e9dfde82a3db0afe247eb383454` — `fix(takeoff): make trace smoke nullable-correct`. This only annotates the Count-only optional expected raw-unit test parameter as `string?` after `Directory.Build.props` strict nullable/warnings-as-errors policy was re-read; it does not change production behavior or overlap the separate MTR foundation nullable claim.
- Exact GitHub compare confirmed the implementation changes only the four reserved source/test surfaces: `QuantityEngine.cs`, new `TakeoffResultWithTrace.cs`, new `TakeoffMeasurementTraceSmoke.cs`, and one line in `SmokeTestRegistration.cs`.
- Current-main readback confirmed `CalculateWithTrace(...)` calls `Calculate(...)` before trace construction; trace gross/net/unit are sourced from the returned canonical result, and raw metric facts use the original snapshot/drawing-unit identity without unit recomputation.
- The focused smoke compares `Calculate(...)` and `CalculateWithTrace(...).Result` for Count/Length/Area/Volume, checks source Handle identity, raw drawing-unit facts, conversion-path assumption, empty adjustments/no rounding, and requires the same exception type/message for missing metrics and invalid drawing-unit validation.

## Validation actually executed

- Executed: refresh/reconcile current `main` before claim and implementation; post-claim reachability/overlap check; current-main compare/readback of all implementation files; exact implementation-file scope inspection; `Directory.Build.props` nullable/warnings-as-errors policy inspection; post-follow-up smoke readback showing the nullable-correct `string?` parameter.
- Not executed in this connector-only lane: `dotnet build`, `QS3D.Core.SmokeTests` executable, GitHub Actions, BricsCAD V25/V26 runtime, or licensed/native qualification. No PASS is claimed for any unexecuted gate.
- Remaining executable gate: run the registered Core smoke suite (and normal Core build) in a checkout with the .NET SDK when execution is explicitly assigned/available. The independent active MTR foundation nullable claim must also land before strict whole-Core compilation can be expected to clear the known MTR-01 nullable errors.

## Completion condition

Satisfied for this narrow MTR-03 raw-takeoff projection: claim-first coordination was observed, canonical calculation remains single-source, trace output reconciles by construction to the existing result, regression is committed/registered and nullable-correct under repository policy, the exact source/test surface is present on `main`, and unexecuted runtime/build evidence is not represented as PASS. Category-specific semantic regenerator trace coverage remains separate future MTR-03 claims and must continue to respect active/blocking native ownership.
