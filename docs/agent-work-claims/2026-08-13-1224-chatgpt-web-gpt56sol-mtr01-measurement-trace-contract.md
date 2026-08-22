# Work claim — MTR-01 canonical MeasurementTrace foundation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr01-measurement-trace-20260813-1224`
- Registered: `2026-08-13T12:24:00+07:00`
- Baseline main SHA: `9c0a25d62b5d1807d6586557370448e0e68bec7d`
- Priority: `MTR-01 / P0` — explainable quantity foundation

## Confirmed gap

Current `QS3D.Core.Takeoff.TakeoffResult` exposes only the measured handle/kind/value/unit result and current source search/readback contains no `MeasurementTrace` contract. The official workstream explicitly lists MTR-01 as a P0 foundation before trace projection, snapshots/deltas, BOQ and estimate lanes. This claim establishes only the missing deterministic Core representation; it does not add a second calculation engine.

## Reserved scope

- new `src/QS3D.Core/Measurement/MeasurementTrace.cs` — immutable trace contract plus its fact/adjustment value objects and deterministic canonical representation/equality/invariants;
- new `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs` — focused CAD-independent regression;
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` — one registration call only;
- this claim file.

## Contract boundary

- Record semantic/source identity, quantity key, canonical input facts, gross/net result, explicit unit, optional rule identity/version, explicit rounding policy, adjustments, warnings and assumptions.
- Validate finite numeric state and canonical text/unit identity; snapshot caller collections so later mutation cannot alter a trace.
- Canonical representation must be culture-invariant and deterministic; equality/hash semantics must reflect the canonical content.
- The trace records/explains supplied values only. It must not calculate quantity formulas, re-run category math, mutate project state, or become a second quantity engine.

## Excluded scope

- No `QuantityEngine`, `QuantityRuleEngine`, semantic regenerator, report/UI, BOQ, estimate, snapshot/delta, persistence/schema, BricsCAD adapter or native-runtime changes.
- No category migration or MTR-03 projection work.
- No GitHub Actions dispatch and no BricsCAD native PASS claim.

## Validation plan

- Add a deterministic smoke proving invariant/culture-independent canonical output, structural equality, detached collection snapshots, optional rule pair semantics and finite/canonical rejection.
- Read back the source/smoke on current `main` after the implementation commit.
- Connector-only source/readback is not an executable .NET test run; no build/smoke/runtime PASS will be claimed unless actually executed.
