# Work claim — Beam Stirrup core generated metadata canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-core-metadata-canonicality`
- Registered: `2026-08-12T11:53:00+07:00`
- Completed: `2026-08-12T11:57:00+07:00`
- Baseline main SHA: `1bf530136cfff6a7ae8e99074d5089fa1b3662fd`
- Priority: P1 — generated Beam Stirrup core metadata must preserve writer-owned serialization.
- Task Key: `CORE-BEAM-STIRRUP-CORE-METADATA-CANONICALITY`

## Confirmed defect

`BeamStirrupSolidBuilder` persists `GeneratedBeamStirrupCount` with invariant `int.ToString`, `GeneratedBeamStirrupDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`, and `GeneratedBeamStirrupMode` as exact planner-owned literals. Health previously accepted alternate parsed spellings such as `01`, `10.0`, or padded/case-varied recognized modes.

## Integrated contract

- A count that parses and equals the valid handle count must use exact invariant integer spelling or emits `BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive diameter must use exact round-trip invariant spelling or emits `BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- A present recognized mode alias must use the exact writer-owned literal or emits `BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL` as Error.
- Existing count mismatch, diameter invalid, unsupported/missing-mode and advanced-mode semantics retain precedence.
- Exact writer-owned values preserve existing behavior.

## Evidence

- PR: `#848`
- Squash merge: `2786cc9e2a9ced9a5e6e18ee7a7b1c2e36d8a55a`
- Source read back from `main`: `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- Regression read back from `main`: `tests/QS3D.Core.SmokeTests/BeamStirrupCoreMetadataCanonicalitySmoke.cs`
- Regression pins count/diameter/mode aliases, invalid/mismatch precedence and canonical controls.

## Exclusions preserved

No Beam Stirrup planner/builder, actual-spacing validation, advanced centerline/polyline/bend/hook/angle metadata, handle ownership, native CAD generation, persistence format, command wrapper or BricsCAD runtime changes were made.

## Validation boundary

Source and smoke were read back from remote `main` after merge. No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS is claimed without execution.
