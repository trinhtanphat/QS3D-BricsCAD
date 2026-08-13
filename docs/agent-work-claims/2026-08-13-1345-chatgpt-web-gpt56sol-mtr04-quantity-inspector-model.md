# Work claim — MTR-04 “Why this quantity?” inspector model

- Status: `COMPLETED`
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

- `src/QS3D.Core/Measurement/MeasurementTraceInspector.cs` — immutable/read-only explanation projection only;
- `tests/QS3D.Core.SmokeTests/MeasurementTraceInspectorSmoke.cs` — focused projection/parity/detachment regression;
- `tests/QS3D.Core.SmokeTests/MeasurementTraceInspectorRegistration.cs` — ModuleInitializer registration;
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

## Completion record

- Claim-only commit: `e648cc3961a9a8e9363bfc653d87efa2aae69c7d`.
- Core projection commit: `615620c2da0e2375c7e99b783bf25e115ae11f0b`.
- Focused smoke commit: `192b681e791400d217e4ea2ece33b8a1f7e2138b`.
- Smoke registration / final implementation SHA: `6788c44c3e8d255927296a35fe3de3a8e1289dd8`.
- Re-fetched `MeasurementTraceInspector.cs` and `MeasurementTraceInspectorSmoke.cs` from current `main`; contents match intended scope.
- Source-level review confirms the projection copies canonical values, facts, adjustments, rule metadata, warnings and assumptions without invoking a quantity engine or arithmetic path. The regression deliberately preserves a non-reconciled canonical `NetValue` to guard against hidden recomputation.
- Smoke registration follows the repository's current `ModuleInitializer` convention.
- Local executable validation: `NOT_RUN` — this runtime has no `dotnet`, `csc`, `msbuild` or `xbuild`, and no repository checkout is available for compilation.
- GitHub Actions: `NOT_RUN` by policy.
- BricsCAD native qualification: `NOT_RUN`; no native PASS is claimed.

## Completion condition

Satisfied for the claimed Core projection lane: a claim-first deterministic inspector projection and focused auto-registered smoke are present on `main`, quantity arithmetic and host UI remain untouched, and the exact implementation SHAs plus actual validation limitations are recorded here.
