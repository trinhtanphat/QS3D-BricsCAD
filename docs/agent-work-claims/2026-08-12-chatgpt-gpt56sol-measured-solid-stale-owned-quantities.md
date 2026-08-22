# Work claim — measured-solid stale owned quantities

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-measured-stale`
- Registered: `2026-08-12T08:24:00+07:00`
- Completed: `2026-08-12T08:28:00+07:00`
- Baseline main SHA: `dedb37d38ed485c93c216b9b271feaea26df6732`
- Claim commit: `c3800ded653e1d69e41a4bae02cb146b8af132d7`
- Implementation commit: `47601ab56cd9fa8cc3faaf97d20e7b954ed6a78f`
- Regression-test commit: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Final pushed product/test SHA: `4e74d36cbfcec75998cbca55f14fc6a858aea7b1`
- Priority: `Correctness/lifecycle hardening discovered during requested full repository review; prevent source-derived measured quantities from surviving after their measured properties are removed.`

## Reserved scope

Make `MeasuredSolidQuantityPolicy.Apply` retract only the policy-owned derived quantities whose source properties are no longer present, while preserving the existing atomic validation contract and leaving standard regenerator-owned gross/net quantities alone. Add focused lifecycle regression coverage.

## Implemented

- Preserved validate-before-mutate parsing of every applicable measured input.
- Retract stale `MeasuredSurfaceAreaM2` when `MeasuredSolidSurfaceAreaM2` is absent.
- Retract stale `MeasuredSolidVolumeM3` when no applicable measured-volume source is present.
- Count stale-key retraction as handled policy work.
- Preserve `GrossVolumeM3` / `NetVolumeM3`; they remain shared outputs owned/recomputed by the normal category regenerator before measured overrides.

## Changed surfaces

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`
- `tests/QS3D.Core.SmokeTests/MeasuredSolidQuantityAtomicitySmoke.cs`

## Regression coverage

- Existing malformed-volume atomicity remains covered and still observes no mutation before failure.
- Existing valid surface/volume application remains covered.
- Existing unsupported-category volume property remains ignored when no policy-owned stale quantity exists.
- New direct-policy case proves missing source properties retract only the two policy-owned `Measured*` quantities while preserving gross/net values.
- New full regeneration lifecycle case uses Earthwork: measured volume overrides the category-computed volume, then source-property removal causes the measured keys to disappear and `GrossVolumeM3` / `NetVolumeM3` to fall back to the standard Earthwork computation.

## Excluded scope

- measured CAD extraction / B4D scanning
- category regenerator formulas
- gross/net quantity semantics beyond preserving their existing ownership
- structural geometry, reporting/export, UI, persistence schema
- GitHub Actions or licensed BricsCAD runtime qualification

## Validation performed

- Re-read current `MeasuredSolidQuantityPolicy.cs` and its existing atomicity smoke before implementation.
- Verified `RegenerationEngine` invokes the category regenerator before `MeasuredSolidQuantityPolicy.Apply`, which is the ownership ordering required for safe gross/net fallback.
- Verified `GenericTakeoffRegenerator` is present in `StructuralRegenerator.cs`; the earlier filename-based suspicion of a missing type was explicitly rejected and no build workaround was committed.
- Source and test writes were SHA-guarded and re-read from current `main` after push.
- No GitHub Actions workflow was dispatched or re-run. No licensed BricsCAD V25 runtime PASS is claimed.

## Outcome

Removing measured-solid source metrics can no longer leave the policy's own derived `Measured*` quantities stale. Normal category quantities remain intact/fall back through their existing regenerators, and the lane is closed without force-push or concurrent-work overwrite.
