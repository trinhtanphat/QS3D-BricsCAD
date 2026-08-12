# Work claim — Opening positive physical dimensions

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-opening-positive-dimensions`
- Registered: `2026-08-12T09:23:00+07:00`
- Completed: `2026-08-12T09:28:00+07:00`
- Baseline main SHA: `ee4375ad4ae975a273915947655bc16ee22af959`
- Claim commit: `f874004168f4618fe45ed2551ff7afc1877447d9`
- Implementation commit: `469d31736a0881eb829f2b345ea4fe28dd1944c4`
- Smoke commit: `ff86f68cdc617a9be7e86907b1aac4fbfabcc0f6`
- Smoke registration commit: `53a4490f245774e9253d24ba70799b4311ff7e12`
- Priority: P1 — prevent finite but non-physical opening dimensions from entering Core state.
- Task Key: `CORE-OPENING-POSITIVE-PHYSICAL-DIMENSIONS`

## Confirmed defect

`OpeningPropertySet` starts with positive physical defaults (`WidthMm = 900`, `HeightMm = 2200`, `ThicknessMm = 110`) and rejected NaN/Infinity, but its setters still accepted zero and negative finite values. A caller could therefore turn an otherwise valid opening property set into a non-physical width, height or thickness without any Core-level rejection.

`SillOffsetMm` is an offset rather than a size and remains allowed to be zero or negative as long as it is finite.

## Completed implementation

- `WidthMm`, `HeightMm` and `ThicknessMm` now use `RequirePositiveFinite(...)`.
- Positive-size validation first reuses the existing finite-value gate and then rejects values `<= 0`.
- `SillOffsetMm` remains on `RequireFinite(...)`, preserving negative/zero finite offset behavior.
- Validation occurs before assignment, so a rejected setter cannot replace the previous valid backing value.
- Existing positive defaults and `BottomLevel` semantics are unchanged.

## Regression evidence

`OpeningPropertySetPositiveDimensionsSmoke` is auto-registered through a module initializer and covers:

- positive default width/height/thickness;
- zero and negative rejection for width/height/thickness;
- preservation of prior valid values after rejected setters;
- negative and zero finite sill offsets remaining accepted;
- existing NaN/Infinity rejection and state preservation.

## Validation performed

- Re-fetched the source after claim publication and edited exact blob `3aff7dd97ad301bbd3b4e24a8a20c373142d3698`.
- Read back current `main` source blob `5008a5ee699325c74add2d25c306fea642bc31af`.
- Read back smoke blob `53c6f6ad71c3557ed33001e0630d85c573d25c0b` and registration blob `9b55f907aa2e89cc7ab7da5a6cf993d9e1abf6fe`.
- Inspected implementation commit `469d31736a0881eb829f2b345ea4fe28dd1944c4`; its diff only changes the reserved domain source and contains 11 additions / 3 deletions.
- GitHub compare showed implementation and final smoke-registration commits are ancestors of current `main` (`behind_by = 0`).

## Validation boundary

No GitHub Actions were dispatched. No local/full build, executable smoke run or licensed BricsCAD V25/V26 runtime PASS is claimed in this connector-only lane.

## Excluded scope

No native BricsCAD opening creation, host association, boolean/materialization, recognition, UI, persistence schema or automatic repair behavior was changed.

## Completion condition

Completed: Core opening physical-size setters fail closed on zero/negative values while sill offsets preserve existing semantics, focused regression evidence is committed on `main`, and the claim is closed with exact integration evidence.