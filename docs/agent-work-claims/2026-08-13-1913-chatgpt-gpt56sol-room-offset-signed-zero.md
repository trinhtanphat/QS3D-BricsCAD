# Work claim — RoomPropertySet offset signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-room-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:13:00+07:00`
- Completed: `2026-08-13T19:15:00+07:00`
- Baseline main SHA: `2a15daa657018d01f781c2409041bb32ef905fc7`
- Priority: P0 deterministic Core room-metric canonicality.

## Confirmed defect

`RoomPropertySet.BaseOffsetMm` and `RoomPropertySet.TopOffsetMm` legitimately accept finite zero and negative values, but both setters delegated to `RequireFinite()`, which rejected NaN/infinity and returned the raw finite `double`. IEEE-754 `-0d` could therefore be stored and exposed unchanged. This was inconsistent with the canonical positive-zero contract already enforced for accepted zero-valued Core numeric state.

## Implemented scope

- `src/QS3D.Core/Domain/RoomPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/RoomPropertySetSignedZeroSmoke.cs`
- this claim file

## Implemented change

- `RequireFinite()` still rejects NaN/infinity and now canonicalizes every accepted finite zero to literal `+0d`.
- Both `BaseOffsetMm` and `TopOffsetMm` therefore store/expose canonical positive zero for `-0d` input.
- Legal negative and positive nonzero room offsets remain unchanged.
- Existing room-generation Boolean defaults remain unchanged.
- New registered `RoomPropertySetSignedZeroSmoke` bit-checks both offsets using `BitConverter.DoubleToInt64Bits`, covers negative/positive nonzero values, non-finite refusal, and existing Boolean defaults.

## Excluded scope

- no changes to Opening/Wall/Element numeric contracts;
- no formula/measurement/cost changes;
- no persistence/UI/native BricsCAD changes;
- no overlap with ACTIVE CST-04 or Formula evaluator claims;
- no GitHub Actions, packaging, release or licensed runtime qualification.

## Coordination / moving-main reconciliation

- Exact recent commit searches for `RoomPropertySet signed zero` and `Room offset signed-zero` returned no competing lane before claim.
- Claim commit: `3a3dac9eae0330a7ac875afe539fb151d84187d2`.
- Production fix: `e845524e3861e6f00d22839fe489bc89bb69e90e` — `fix(domain): canonicalize Room offset signed zero`.
- Focused regression: `6adce93aad796753e5ef3ba241fe96424ac6c864` — `test(domain): guard Room offset signed zero`.
- Main remained exactly at the regression commit during source/test readback; no concurrent commit touched the reserved Room source/test before closeout.

## Validation actually performed

- Exact production readback confirmed blob `6ecae4faf2aeb380cf3ebf5b7b4c0729c0dbb8e5`; the production diff is limited to zero canonicalization in `RequireFinite()`.
- Exact regression readback confirmed blob `188415f78b6acf6a9bab5508d786f55424f66c87`; it contains bit-level zero assertions and nonzero/non-finite/default-state sanity coverage.
- The smoke uses the repository's existing `[ModuleInitializer]` registration pattern, matching the already-registered Opening signed-zero regression convention.
- No managed build/smoke, GitHub Actions, adapter build, packaging or licensed BricsCAD runtime execution was performed in this connector-only lane; no execution PASS is claimed.

## Completion condition

Satisfied for this bounded Core source/static lane: both accepted room offsets store/expose canonical positive zero for zero-valued inputs, legal nonzero offsets and non-finite refusal remain unchanged, focused registered regression is on current `main`, exact remote readback is verified, and unavailable managed/native gates remain explicitly unclaimed.
