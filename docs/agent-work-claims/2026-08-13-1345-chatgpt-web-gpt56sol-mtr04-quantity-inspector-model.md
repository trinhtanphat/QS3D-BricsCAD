# Work claim — MTR-04 “Why this quantity?” inspector model

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr04-quantity-inspector-20260813-1345`
- Registered: `2026-08-13T13:45:00+07:00`
- Baseline main SHA: `328ddd6c11458a8bbfb6f887d386cfdbb747eb2e`
- Priority: `MTR-04 / P0-P1` — expose canonical MeasurementTrace as a deterministic explanation model without creating another quantity engine

## Confirmed gap

MTR-01 provides the canonical `MeasurementTrace` contract and MTR-03 projects existing canonical takeoff results into traces. Current source/history contains semantic/property selection inspectors, but no quantity-explanation/“Why this quantity?” projection over `MeasurementTrace`. The roadmap explicitly separates Core/view-model preparation from host UI.

## Reserved scope

Add one pure-Core, read-only inspector projection that consumes an existing `MeasurementTrace` and exposes its canonical explanation data without recomputation:

- semantic/source identity and quantity key;
- gross/net/unit/rounding policy;
- rule ID/version when present;
- detached input-fact rows;
- detached deduction/addition rows including reason/source and adjustment rule ID/version when present;
- warnings and assumptions already recorded by the trace;
- deterministic ordering inherited from the canonical trace contract.

The projection must never derive a replacement quantity, formula, deduction or conversion. It only exposes canonical trace content for a future host UI/report consumer.

## Expected surfaces

- new `src/QS3D.Core/Measurement/MeasurementTraceInspector.cs` — immutable/read-only explanation projection only;
- new `tests/QS3D.Core.SmokeTests/MeasurementTraceInspectorSmoke.cs` — focused projection/parity/detachment regression;
- new `tests/QS3D.Core.SmokeTests/MeasurementTraceInspectorRegistration.cs` — ModuleInitializer registration if current smoke convention supports it;
- this claim file.

## Excluded scope

- No changes to canonical quantity calculation, QuantityEngine arithmetic, measurement rules, trace generation or trace serialization.
- No BricsCAD V25/V26 adapters, WPF/property palette/selection inspector UI or native qualification.
- No report/XLSX/DWG renderer-specific math or persistence/schema changes.
- No REV-03 snapshot/delta-reason work, MAP coverage/mapping work, rate/estimate work or Curtain/LOCAL lanes.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Re-fetch current `main` after this claim-only commit and recheck newly published claims before source changes.
- Smoke proves exact field parity from canonical trace, rule/version exposure, deduction/addition metadata, warnings/assumptions, stable canonical ordering, and that returned collections are detached/read-only projections rather than a mutable second source of truth.
- Re-fetch exact implementation files/commits from GitHub before closeout.
- Connector-only source inspection is not an executable `.NET` smoke/build run; unexecuted gates remain `NOT_RUN`.

## Coordination

- MTR-01/MTR-02/MTR-03 are completed prerequisites and remain unchanged.
- Existing semantic/property selection inspector capability is a neighboring UI/property lane and is explicitly not owned here.
- REV-03A is treated as owned and excluded until its claim closes.
- LOCAL-003 and Curtain native qualification remain separate reserved/runtime lanes.

## Completion condition

A claim-first deterministic Core inspector projection + focused auto-registered smoke is present on current `main`, no quantity arithmetic or host UI is duplicated, and this claim is closed with exact pushed SHAs plus actual validation evidence.
