# Work claim — EntitySnapshot non-negative metric integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-nonnegative-metrics-20260812-0947`
- Registered: `2026-08-12T09:47:00+07:00`
- Baseline main SHA observed before reservation: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Priority: P1 — captured CAD measurement magnitudes must fail closed before recognition/capture consumes malformed values.

## Confirmed defect

`EntitySnapshot` represents CAD measurement magnitudes through nullable Length, Area, SurfaceArea and Volume fields. Their shared setter guard currently rejects only NaN/Infinity, so negative finite lengths/areas/volumes can be stored in a semantic snapshot even though downstream capture eligibility explicitly requires positive primary metrics. This permits malformed source state to survive past the model boundary and be interpreted inconsistently by later recognition/capture paths.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- one isolated Core smoke file for snapshot metric bounds
- this claim file for close-out

## Contract

- Reject negative finite values for LengthDrawingUnits, AreaDrawingUnitsSquared, SurfaceAreaDrawingUnitsSquared and VolumeDrawingUnitsCubed when provided.
- Preserve `null` as "measurement unavailable" and preserve zero as a finite non-negative measurement that downstream readiness may still classify as insufficient.
- Preserve positive values and existing NaN/Infinity rejection.
- Do not change recognition scoring, proxy capture readiness thresholds, adapters, CAD runtime behavior, unit conversion, or generated-output ownership semantics.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

The model boundary rejects negative measurement magnitudes, deterministic isolated Core smoke coverage is integrated on current `main`, resulting source/test are re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
