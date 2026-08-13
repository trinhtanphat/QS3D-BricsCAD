# Work claim — WallPropertySet offset signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-wall-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:16:00+07:00`
- Baseline main SHA: `e62fbce5562b7e0156d669f742e0b6c141ef1701`
- Priority: P0 deterministic Core wall-metric canonicality.

## Confirmed defect

`WallPropertySet.AxisToLeftMm`, `AxisToRightMm`, `BaseOffsetMm`, and `TopOffsetMm` legitimately accept finite zero and negative values, but their shared `RequireFinite()` helper rejects NaN/infinity and returns the raw finite `double`. IEEE-754 `-0d` can therefore be stored and exposed unchanged. `ThicknessMm` delegates through the same helper but remains strictly positive via `RequirePositiveFinite()`.

## Reserved scope

- `src/QS3D.Core/Domain/WallPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/WallPropertySetSignedZeroSmoke.cs` (new focused registered smoke)
- this claim file for closeout

## Intended change

- canonicalize every accepted finite zero returned by `WallPropertySet.RequireFinite()` to literal `+0d`;
- preserve NaN/infinity rejection;
- preserve legal negative/positive nonzero axis and vertical offsets;
- preserve strictly-positive thickness semantics;
- preserve existing Boolean and level defaults;
- add bit-level regression coverage for all four zero-accepting numeric properties plus nonzero/non-finite/thickness sanity cases.

## Excluded scope

- no wall quantity/calculation changes;
- no Room/Opening/Element changes;
- no formula/measurement/cost changes;
- no persistence/UI/native BricsCAD changes;
- no overlap with ACTIVE CST-04 or Formula evaluator claims;
- no GitHub Actions, packaging, release or licensed runtime qualification.

## Coordination

- Exact recent commit searches for `WallPropertySet signed zero` and `Wall offset signed-zero` returned no competing lane immediately before claim.
- Baseline source blob: `b7f1192e6d940c9bfe137b5f452ad4ecbb5f98d9`.
- Prior `wall quantity signed-zero` work is a separate calculation lane and does not touch `WallPropertySet`.

## Validation plan

- refresh `main` after claim and recheck WallPropertySet before source mutation;
- keep production change to zero canonicalization only;
- add focused bit-level smoke using `BitConverter.DoubleToInt64Bits`;
- re-fetch exact source/test blobs and reconcile moving-main ancestry before closeout;
- managed/native execution remains `NOT_RUN` if unavailable; do not fabricate PASS.

## Completion condition

All four zero-accepting wall metrics store/expose canonical positive zero, legal nonzero offsets and strict-positive thickness/non-finite refusal remain unchanged, focused registered regression is on current `main`, exact remote readback is verified, and this claim closes `COMPLETED` with truthful validation boundaries.
