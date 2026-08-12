# Work claim — EntitySnapshot non-negative metric integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-nonnegative-metrics-20260812-0947`
- Registered: `2026-08-12T09:47:00+07:00`
- Completed: `2026-08-12T09:52:00+07:00`
- Baseline main SHA observed before reservation: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Pull Request: `#718`
- Reviewed head: `b611496f33cdddec1812d5a3cfd76fadd7ff1c93`
- Merge SHA: `e9eeb5b02cc858f6720746f0f0c84cda6b30b8a4`
- Priority: P1 — captured CAD measurement magnitudes must fail closed before recognition/capture consumes malformed values.

## Confirmed defect

`EntitySnapshot` represents CAD measurement magnitudes through nullable Length, Area, SurfaceArea and Volume fields. Their shared setter guard rejected only NaN/Infinity, so negative finite measurements could survive the model boundary even though downstream capture eligibility requires positive primary metrics.

## Completed contract

- Negative finite values are rejected for LengthDrawingUnits, AreaDrawingUnitsSquared, SurfaceAreaDrawingUnitsSquared and VolumeDrawingUnitsCubed.
- `null` remains "measurement unavailable" and zero remains a valid finite non-negative measurement.
- Positive values and existing NaN/Infinity rejection remain unchanged.
- Recognition scoring, proxy capture readiness thresholds, adapters, unit conversion and generated ownership semantics were not changed.
- Focused ModuleInitializer smoke coverage exercises all four setters across null/zero/positive/negative/non-finite values.

## Evidence

- PR #718 exact patch reviewed.
- Moving-main comparison from PR base showed no overlap with `EntitySnapshot.cs` or the smoke.
- Squash merge: `e9eeb5b02cc858f6720746f0f0c84cda6b30b8a4`.

## Validation boundary

No GitHub Actions/build/release dispatch occurred. No local/full .NET build or licensed BricsCAD runtime PASS is claimed.
