# Work claim — measured-solid stale owned quantities

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-measured-stale`
- Registered: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `dedb37d38ed485c93c216b9b271feaea26df6732`
- Priority: `Correctness/lifecycle hardening discovered during requested full repository review; prevent source-derived measured quantities from surviving after their measured properties are removed.`

## Reserved scope

Make `MeasuredSolidQuantityPolicy.Apply` retract only the policy-owned derived quantities whose source properties are no longer present, while preserving the existing atomic validation contract and leaving standard regenerator-owned gross/net quantities alone. Add focused lifecycle regression coverage.

## Expected surfaces

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`
- `tests/QS3D.Core.SmokeTests/MeasuredSolidQuantityAtomicitySmoke.cs`

## Exact ownership boundary

May remove only:

- `MeasuredSurfaceAreaM2` when `MeasuredSolidSurfaceAreaM2` is absent;
- `MeasuredSolidVolumeM3` when no applicable measured-volume source is present.

Must not blindly remove `GrossVolumeM3` or `NetVolumeM3`; those are shared outputs recomputed by normal category regenerators before measured overrides.

## Excluded scope

- measured CAD extraction / B4D scanning
- category regenerator formulas
- gross/net quantity semantics beyond preserving their existing ownership
- structural geometry, reporting/export, UI, persistence schema
- GitHub Actions or licensed BricsCAD runtime qualification

## Validation plan

- Preserve validate-before-mutate behavior for malformed applicable measured inputs.
- Add direct-policy regression proving stale policy-owned keys are removed when source properties disappear.
- Add regeneration lifecycle coverage if the existing smoke harness supports it without broadening scope, proving standard gross/net quantities fall back to category regeneration rather than being erased.
- Re-read latest main before each write and use SHA-guarded updates.

## Completion condition

Claim is on `main` before product changes; implementation and focused regression are pushed without overwriting concurrent work; claim is closed `COMPLETED` with exact commit/evidence notes.
