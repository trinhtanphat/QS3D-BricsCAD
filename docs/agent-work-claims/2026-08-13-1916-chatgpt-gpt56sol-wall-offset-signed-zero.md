# Work claim — WallPropertySet offset signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-wall-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:16:00+07:00`
- Completed: `2026-08-13T19:18:00+07:00`
- Baseline main SHA: `e62fbce5562b7e0156d669f742e0b6c141ef1701`
- Priority: P0 deterministic Core wall-metric canonicality.

## Confirmed defect

`WallPropertySet.AxisToLeftMm`, `AxisToRightMm`, `BaseOffsetMm`, and `TopOffsetMm` legitimately accept finite zero and negative values, but their shared `RequireFinite()` helper rejected NaN/infinity and returned the raw finite `double`. IEEE-754 `-0d` could therefore be stored and exposed unchanged. `ThicknessMm` delegates through the same helper but remains strictly positive via `RequirePositiveFinite()`.

## Implemented scope

- `src/QS3D.Core/Domain/WallPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/WallPropertySetSignedZeroSmoke.cs`
- this claim file

## Implemented change

- `RequireFinite()` still rejects NaN/infinity and now canonicalizes every accepted finite zero to literal `+0d`.
- `AxisToLeftMm`, `AxisToRightMm`, `BaseOffsetMm`, and `TopOffsetMm` therefore store/expose canonical positive zero for `-0d` input.
- Legal negative/positive nonzero axis and vertical offsets remain unchanged.
- `RequirePositiveFinite()` still applies `<= 0d` after normalization, so `ThicknessMm` continues to reject both `+0d` and `-0d` and remains strictly positive.
- Existing Boolean and level defaults remain unchanged.
- New registered `WallPropertySetSignedZeroSmoke` bit-checks all four zero-accepting properties and covers nonzero offsets, strict-positive thickness, non-finite refusal and defaults.

## Excluded scope

- no wall quantity/calculation changes;
- no Room/Opening/Element changes;
- no formula/measurement/cost changes;
- no persistence/UI/native BricsCAD changes;
- no overlap with ACTIVE CST-04 or Formula evaluator claims;
- no GitHub Actions, packaging, release or licensed runtime qualification.

## Coordination / moving-main reconciliation

- Exact recent commit searches for `WallPropertySet signed zero` and `Wall offset signed-zero` returned no competing lane before claim.
- Claim commit: `cdf9c7aaddd2bf00cb50b8e7f2b3eb0d3fdcbafc`.
- Production fix: `0d63b432496e911d851f5b82e729c91d1dce78fd` — `fix(domain): canonicalize Wall offset signed zero`.
- Focused regression: `429197365078757ca078b2b3bcf6e111286da229` — `test(domain): guard Wall offset signed zero`.
- Main remained exactly at the regression commit during source/test readback; no concurrent commit touched the reserved Wall source/test before closeout.
- Prior `wall quantity signed-zero` work remains a separate calculation lane.

## Validation actually performed

- Exact production readback confirmed blob `78012c0df1dc3e1d559f0b2293cebea38d021ecf`; production diff is limited to zero canonicalization in `RequireFinite()`.
- Exact regression readback confirmed blob `394de12a7b08f508f293e87bade8a0f585685512`; it contains bit-level zero assertions plus nonzero/thickness/non-finite/default-state sanity coverage.
- The smoke follows the repository's existing `[ModuleInitializer]` registration pattern.
- No managed build/smoke, GitHub Actions, adapter build, packaging or licensed BricsCAD runtime execution was performed in this connector-only lane; no execution PASS is claimed.

## Completion condition

Satisfied for this bounded Core source/static lane: all four zero-accepting wall metrics store/expose canonical positive zero, legal nonzero offsets and strict-positive thickness/non-finite refusal remain unchanged, focused registered regression is on current `main`, exact remote readback is verified, and unavailable managed/native gates remain explicitly unclaimed.
