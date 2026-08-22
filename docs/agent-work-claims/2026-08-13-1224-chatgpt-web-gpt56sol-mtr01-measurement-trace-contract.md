# Work claim — MTR-01 canonical MeasurementTrace foundation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr01-measurement-trace-20260813-1224`
- Registered: `2026-08-13T12:24:00+07:00`
- Baseline main SHA: `9c0a25d62b5d1807d6586557370448e0e68bec7d`
- Priority: `MTR-01 / P0` — explainable quantity foundation

## Confirmed gap

Current `QS3D.Core.Takeoff.TakeoffResult` exposed only the measured handle/kind/value/unit result and source/history inspection found no `MeasurementTrace` contract before this lane. The official workstream lists MTR-01 as a P0 foundation before trace projection, snapshots/deltas, BOQ and estimate lanes. This claim established only the missing deterministic Core representation; it did not add a second calculation engine.

## Reserved scope

- new `src/QS3D.Core/Measurement/MeasurementTrace.cs` — immutable trace contract plus fact/adjustment value objects and deterministic canonical representation/equality/invariants;
- new `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs` — focused CAD-independent regression;
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — one registration call only;
- this claim file.

## Contract boundary

- Record semantic/source identity, quantity key, canonical input facts, gross/net result, explicit unit, optional rule identity/version, explicit rounding policy, adjustments, warnings and assumptions.
- Validate finite numeric state and canonical text/unit identity; snapshot caller collections so later mutation cannot alter a trace.
- Canonical representation is culture-invariant and deterministic; equality/hash semantics reflect canonical content and do not depend on runtime-randomized string hashing.
- The trace records/explains supplied values only. It does not calculate quantity formulas, re-run category math, mutate project state, or become a second quantity engine.

## Excluded scope

- No `QuantityEngine`, `QuantityRuleEngine`, semantic regenerator, report/UI, BOQ, estimate, snapshot/delta, persistence/schema, BricsCAD adapter or native-runtime changes.
- No category migration or MTR-03 projection work.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.

## Completion

- Claim-only commit on `main`: `12c21ea2ccc681f3fa3e0d0a2f6f03a0ec7c354c`.
- Implementation + deterministic smoke commit on `main`: `cb567dfd679b41db17dd4819a8161ff3b560a8e6` — `feat(measurement): add canonical measurement trace contract`.
- GitHub compare/readback confirmed that implementation commit changes exactly the three reserved source/test surfaces: the new Core contract, the new focused smoke, and one `SmokeTestRegistration.RunAll()` call.
- GitHub current-main readback confirmed the immutable trace contract and focused smoke source are present after the implementation push. The smoke covers culture/input-order canonicalization, structural equality/hash parity, detached collection snapshots, optional rule ID/version pair semantics, finite numeric rejection, canonical lower-case units and adjustment-unit mismatch rejection.
- No quantity formula was added to the trace contract; gross/net values remain supplied canonical results, and input fact units may legitimately differ from the output quantity unit while adjustment lines must use the output unit.

## Validation actually executed

- Executed: GitHub ref refresh/reconciliation before claim and before source push; current-main ancestry/readback; exact implementation-file diff inspection; source/smoke/registration readback from commit `cb567dfd679b41db17dd4819a8161ff3b560a8e6`.
- Not executed in this connector-only lane: `dotnet build`, the Core smoke executable, GitHub Actions, BricsCAD V25/V26 runtime, or licensed/native qualification. No PASS is claimed for any unexecuted gate.
- Remaining executable gate: run the registered CAD-independent Core smoke suite in an environment with the repository checkout/.NET SDK when such execution is explicitly assigned. No new BricsCAD-native or LOCAL_ONLY scenario is introduced by this pure Core contract.

## Completion condition

Satisfied for the reserved remote/source MTR-01 foundation: the claim was published before implementation, the source/test batch is on current `main` and was read back from GitHub, the lane remains isolated from canonical quantity calculation, and no unexecuted build/runtime evidence is represented as PASS. MTR-03 category projection, persistence/serialization policy beyond the canonical in-memory representation, UI/reporting, snapshots/deltas, BOQ and estimate work remain separate future claims.
