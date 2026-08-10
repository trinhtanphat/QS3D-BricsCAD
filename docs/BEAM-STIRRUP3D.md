# Beam stirrup 3D

Beam stirrup generation is exposed through both `QS3DBEAMSTIRRUP3D` and the compatibility command `QS3DREBARSTIRRUP3D`. Health is available through `QS3DBEAMSTIRRUPHEALTH` and `QS3DREBARSTIRRUPHEALTH`.

## Base inputs

- The selected semantic host must be a Beam backed by exactly one selected live LINE source.
- `RebarStirrupNotation` supplies one diameter plus either a count or spacing, for example `D8@150` or a supported count-style notation.
- `WidthM`, `HeightM`, `RebarStirrupCoverM`, `RebarStirrupEndCoverM` and `BottomOffsetM` are resolved from instance/Family data.
- The current V25 adapter requires a near-horizontal Beam source with `|ΔZ| <= 0.005 m` after unit conversion.

## Bend and hook data are explicit engineering inputs

QS3D does **not** infer a national-code bend radius, hook length or hook angle. Advanced fabrication geometry is used only when the project/Family explicitly provides:

- `RebarStirrupBendRadiusM`
- `RebarStirrupHookLengthM`
- `RebarStirrupHookTailAngleDeg`
- `RebarStirrupMaximumSagittaM` — tessellation quality only, not an engineering default

Engineering inputs default to zero. Therefore an existing project with no bend/hook data keeps the legacy rectangular five-point closed-loop geometry.

When bend radius is positive, `BeamStirrupLayoutPlanner` constructs rounded corners through `BulgeArcTessellator`. When hook length is positive, hook angle must be strictly inside `(0, 180)` degrees and both hook endpoints must remain inside the permitted Beam section envelope. Impossible bend radii, ambiguous distribution input and out-of-envelope hooks are rejected rather than approximated.

## Length and mode metadata

A generated snapshot records:

- `GeneratedBeamStirrupCenterlineLengthM` — exact analytical centerline length;
- `GeneratedBeamStirrupTotalCenterlineLengthM` — exact centerline length multiplied by generated count;
- `GeneratedBeamStirrupPolylineLengthM` — tessellated path length;
- bend radius, hook length and hook-tail angle used for generation;
- one of `Beam.Line.RectangularClosedLoop`, `Beam.Line.RectangularRoundedLoop`, `Beam.Line.RectangularHookedPath`.

The V25 solid builder uses segment overlap only at actual internal joints. For an open hooked path the first and last hook endpoints are not extended outside the designed path just to make Boolean unions easier.

## Ownership, invalidation and health

Generated solids live under `GeneratedBeamStirrupHandles`. Replacement first validates generated-rebar ownership and refuses destructive erase when a tracked handle belongs to another source/generated namespace or no longer points to the expected generated `Solid3d`.

Semantic/source invalidation clears both handles and all derived bend/hook/length metadata. Successful regeneration clears the Beam-stirrup stale flag.

`GeneratedBeamStirrupHealthService` remains backward-compatible with legacy snapshots that predate advanced metadata. Once advanced metadata exists it also validates mode presence, exact-total length consistency, finite/non-negative fabrication fields and mode/bend/hook consistency.

## Validation and runtime boundary

`BeamStirrupLayoutSmoke`, `BeamStirrupBendSmoke` and `BeamStirrupMetadataHealthSmoke` cover deterministic Core/metadata behavior. `scripts/preflight-beam-stirrups.py`, `scripts/preflight-beam-stirrup-bends.py`, `scripts/preflight-smoke-registration.py` and `scripts/preflight-all.py` guard source integration.

These are source/Core gates. Real V25 plugin compilation, `NETLOAD`, generated `Solid3d` Boolean behavior and DWG evidence still require the authorized BricsCAD V25 Windows runtime gate; they must not be inferred from source review alone.