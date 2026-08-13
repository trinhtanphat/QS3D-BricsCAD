# Work claim — RoomPropertySet offset signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-room-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:13:00+07:00`
- Baseline main SHA: `2a15daa657018d01f781c2409041bb32ef905fc7`
- Priority: P0 deterministic Core room-metric canonicality.

## Confirmed defect

`RoomPropertySet.BaseOffsetMm` and `RoomPropertySet.TopOffsetMm` legitimately accept finite zero and negative values, but both setters delegate to `RequireFinite()`, which rejects NaN/infinity and returns the raw finite `double`. IEEE-754 `-0d` can therefore be stored and exposed unchanged. This is inconsistent with the canonical positive-zero contract already being enforced for other accepted zero-valued Core numeric state.

## Reserved scope

- `src/QS3D.Core/Domain/RoomPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/RoomPropertySetSignedZeroSmoke.cs` (new focused registered smoke)
- this claim file for closeout

## Intended change

- canonicalize every accepted finite zero returned by `RoomPropertySet.RequireFinite()` to literal `+0d`;
- preserve NaN/infinity rejection;
- preserve legal negative and positive nonzero room offsets;
- preserve all existing room-generation Boolean defaults/behavior;
- add bit-level regression coverage for both `BaseOffsetMm = -0d` and `TopOffsetMm = -0d`, plus nonzero and non-finite sanity cases.

## Excluded scope

- no changes to Opening/Wall/Element numeric contracts;
- no formula/measurement/cost changes;
- no persistence/UI/native BricsCAD changes;
- no overlap with ACTIVE CST-04 or Formula evaluator claims;
- no GitHub Actions, packaging, release or licensed runtime qualification.

## Coordination

- Exact recent commit searches for `RoomPropertySet signed zero` and `Room offset signed-zero` returned no competing lane immediately before claim.
- Baseline source blob: `38d5c4c2255e758d3bcdd65bb04c5d5d620b57a6`.
- The existing Core smoke project uses module-initializer registration patterns; this lane will follow the same focused pattern as the already-completed Opening signed-zero regression.

## Validation plan

- refresh `main` after claim and recheck RoomPropertySet history before source mutation;
- keep production change to zero canonicalization only;
- add focused bit-level smoke using `BitConverter.DoubleToInt64Bits`;
- re-fetch exact source/test blobs and reconcile moving-main ancestry before closeout;
- managed/native execution remains `NOT_RUN` if no actual toolchain/runtime is available; do not fabricate PASS.

## Completion condition

Both accepted room offsets store/expose canonical positive zero for zero-valued inputs, legal nonzero offsets and non-finite refusal remain unchanged, focused registered regression is on current `main`, exact remote readback is verified, and this claim closes `COMPLETED` with truthful validation boundaries.
