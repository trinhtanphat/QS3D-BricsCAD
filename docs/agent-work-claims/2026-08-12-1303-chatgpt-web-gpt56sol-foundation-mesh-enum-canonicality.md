# Work claim — Foundation mesh enum metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-foundation-mesh-enum-canonicality-20260812-1303`
- Registered: `2026-08-12T13:03:00+07:00`
- Baseline main SHA: `1aeeb42b6f02e4c53b47b0530f4799f8beeba618`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`GeneratedFoundationMeshHealthService` validates `GeneratedFoundationMeshFaces`, `GeneratedFoundationMeshMode`, and present `GeneratedFoundationMeshFootprintMode` with case-insensitive comparisons. Non-canonical persisted tokens such as `bottom`, `foundationmeshxy`, or `rectangleglobalxy` variants can therefore pass health even though these are generated snapshot metadata tokens with canonical spellings. The same health surface already requires canonical invariant text for generated count/handles, so casing drift remains fail-open for these enum-like snapshots.

## Reserved write surface

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedFoundationMeshEnumCanonicalitySmoke.cs`
- this claim file

## Intended change

1. Keep the existing accepted semantic values and legacy missing-footprint compatibility.
2. Require exact ordinal canonical spellings for present Faces, Mode, and FootprintMode tokens.
3. Add focused regression coverage proving lower-case/non-canonical variants fail visible while canonical values remain clean.

## Explicit exclusions

- Foundation mesh generation/native CAD behavior;
- handle/count/ownership validation;
- numeric snapshot validation;
- stale fingerprint semantics;
- unrelated generated-health services, release tooling, persistence, or UI.
