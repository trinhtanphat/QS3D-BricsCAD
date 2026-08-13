# Work claim — MTR foundation nullable contract compile integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr-nullability-20260813-1252`
- Workstream: `MeasurementTrace / P0` — restore strict nullable compile integrity before further Measurement Rules expansion
- Claimed UTC: `2026-08-13T05:52:27Z`
- Last updated UTC: `2026-08-13T05:52:27Z`
- Baseline main SHA: `6b0c522a036891573610a5cc96764ed849aa9900`

## Confirmed defect

`Directory.Build.props` enables nullable reference types and treats warnings as errors. The current canonical `src/QS3D.Core/Measurement/MeasurementTrace.cs` still declares optional/default-null metadata and nullable equality inputs as non-nullable reference types. The current `LOCAL-003` claim is `BLOCKED` after its strict installed-reference V25 build reported 15 nullable compiler errors in this independently merged Core file and explicitly forbids the local/native agent from absorbing the remote-safe Core repair. Current source readback confirms the inconsistent annotations remain present on `main`.

## Planned files

- `src/QS3D.Core/Measurement/MeasurementTrace.cs` — align nullable annotations with the existing runtime contract only; no calculation/canonical-value behavior changes.
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs` — focused regression that preserves the optional-null metadata/equality semantics already supported by the contract; no new smoke registration surface.
- this claim file.

## Scope

- Mark only genuinely optional reference inputs/properties/helpers as nullable and make `IEquatable<T>` / `object.Equals` signatures nullable-correct.
- Preserve current validation, sorting, equality/hash behavior, canonical `MTR1` representation, quantity values, units, rule pair semantics and all calculation ownership.
- Do not add MTR-02 profile/deduction-rule fields in this lane; compile integrity is isolated from feature expansion.

## Initial overlap check

- `MTR-03` is currently `ACTIVE` and reserves `QuantityEngine.cs`, new `TakeoffResultWithTrace.cs`, `TakeoffMeasurementTraceSmoke.cs` and one `SmokeTestRegistration.cs` call; it does not reserve either file in this claim.
- `chatgpt-sol-ui-polish-20260813` is currently `ACTIVE` only on BricsCAD V25 palette/XAML surfaces; no overlap.
- `LOCAL-003` is currently `BLOCKED` on native Level-Z qualification and explicitly identifies this Core nullable defect as an external prerequisite while prohibiting the local worker from repairing it; no local/native file is reserved here.
- Current claim-registry/history audit found no other `ACTIVE`/`BLOCKED` reservation of `MeasurementTrace.cs` or `MeasurementTraceContractSmoke.cs` at the baseline SHA.

## Validation plan

- Re-fetch `main` after this claim-only commit and recheck overlap before source changes.
- Keep the source diff annotation-only except for a focused smoke assertion around optional metadata/equality null semantics.
- Re-fetch/read back the exact implementation diff and final files from current `main` after source push.
- Do not dispatch GitHub Actions.
- Connector-only readback is not a `.NET` build/smoke/native PASS. If no executable checkout/runtime is available in this lane, record that honestly and leave strict `dotnet`/installed-reference V25 rebuild plus BricsCAD qualification as remaining external gates.

## Completion

Pending implementation.
