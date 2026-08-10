# Rebar integration review — 2026-08-10

This note records the current source-level reinforcement integration after the continued BLT-style review. It is intentionally explicit about command names, generated-handle ownership and runtime limits so later agents do not merge different rebar paths accidentally.

## Current generated-rebar paths

### Column longitudinal bars

- Build: `QS3DREBAR3D`
- Health: `QS3DREBARHEALTH`
- Generated ownership: `GeneratedRebarHandles`
- Current native source path is rectangular-column longitudinal reinforcement.

### Beam longitudinal bars

- Build: `QS3DBEAMREBAR3D`
- Uses the protected longitudinal generated-rebar ownership path.
- Supported source is a compatible semantic Beam `LINE`; source/dimensions/distribution remain fail-closed.

### BBS shape bars

- Build: `QS3DREBAR3DSHAPE`
- Health: `QS3DREBARSHAPEHEALTH`
- Generated ownership: `GeneratedShapeRebarHandles`
- Current deterministic shape path supports straight and configured L/U/Z/custom leg/turn definitions when the supplied leg totals agree with BBS cutting length.

### Beam stirrups

- Build: `QS3DREBARSTIRRUP3D`
- Health: `QS3DREBARSTIRRUPHEALTH`
- Generated ownership: `GeneratedBeamStirrupHandles`
- Layout: deterministic beam-stirrup count/spacing planning with section cover, end cover and diameter guards.
- Native geometry: bounded rectangular segmented-cylinder loop solids on supported horizontal Beam `LINE` source.

### Column ties

- Build: `QS3DREBARTIES3D`
- Health: `QS3DREBARTIEHEALTH`
- Generated ownership: `GeneratedTieRebarHandles`
- Layout: deterministic rectangular-column tie distribution with cover/clearance/diameter/count-or-spacing validation.
- Native geometry: bounded rectangular segmented-cylinder loop solids on supported closed rectangular Column footprint source.

## Unified health

`QS3DREBARHEALTHALL` must aggregate every generated-rebar ownership family currently considered part of the product:

- `GeneratedRebarHandles`
- `GeneratedShapeRebarHandles`
- `GeneratedTieRebarHandles`
- `GeneratedBeamStirrupHandles`

The continued review fixed a real integration gap where the unified command originally omitted beam stirrups even though a stirrup-specific health service already existed. The aggregator now invokes `GeneratedBeamStirrupHealthService` and routes `BEAM_STIRRUP_*` issues back to the stirrup handles for Locate.

A dedicated `scripts/preflight-rebar-health-all.py` now guards this contract and also requires Ribbon exposure. Do not rename one generated-handle family or command without updating the aggregator, Ribbon/Hub, preflight and documentation together.

## Host rebuild / invalidation rule

Generated reinforcement must not silently remain attached to stale host geometry. Current source hardening includes ownership-aware invalidation for generated reinforcement families affected by host source/3D rebuild. Destructive replacement must refuse handles protected or owned by another semantic/generated role.

## Deliberate non-claims

The current stirrup/tie source paths are **not fabrication-grade detailing systems**. They intentionally do not invent:

- hook geometry without hook dimensions;
- bend radii without explicit bend rules/radii;
- seismic confinement zones not supplied by project/Family/Instance data;
- code-specific anchorage/lap rules that are absent from the semantic model;
- arbitrary sloped/curved host reinforcement when the adapter only supports a narrower source shape.

Those items require explicit dimensions/rules plus licensed BricsCAD V25/private-DWG validation before they should be implemented as production geometry.

## Runtime gate

All native Solid3d reinforcement paths remain source-level implementations until the newest head is compiled/NETLOAD-tested against the exact licensed BricsCAD V25 runtime and representative private drawings. Earlier CI/Core runs do not prove these newest adapter paths.
