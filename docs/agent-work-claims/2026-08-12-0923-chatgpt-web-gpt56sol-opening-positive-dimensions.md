# Work claim — Opening positive physical dimensions

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-opening-positive-dimensions`
- Registered: `2026-08-12T09:23:00+07:00`
- Baseline main SHA: `ee4375ad4ae975a273915947655bc16ee22af959`
- Priority: P1 — prevent finite but non-physical opening dimensions from entering Core state.
- Task Key: `CORE-OPENING-POSITIVE-PHYSICAL-DIMENSIONS`

## Confirmed defect

`OpeningPropertySet` now starts with positive physical defaults (`WidthMm = 900`, `HeightMm = 2200`, `ThicknessMm = 110`) and rejects NaN/Infinity, but its setters still accept zero and negative finite values. A caller can therefore turn an otherwise valid opening property set into a non-physical width, height or thickness without any Core-level rejection.

`SillOffsetMm` is an offset rather than a size and remains allowed to be zero or negative as long as it is finite.

## Reserved scope

- `src/QS3D.Core/Domain/OpeningPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/OpeningPropertySetPositiveDimensionsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/OpeningPropertySetPositiveDimensionsRegistration.cs`
- this claim file

## Intended contract

- `WidthMm`, `HeightMm` and `ThicknessMm` must be finite and strictly greater than zero.
- `SillOffsetMm` must remain finite; negative and zero offsets stay valid.
- Invalid assignments throw before replacing the previous valid backing value.
- Existing positive defaults and finite-value rejection remain unchanged.

## Excluded scope

- No native BricsCAD opening creation, host association, boolean/materialization, recognition, UI, persistence schema or automatic repair changes.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime qualification claim.

## Validation plan

- Re-fetch the exact source blob after claim publication before editing.
- Add focused auto-registered Core smoke coverage for zero/negative size rejection, positive values, finite offset semantics and failed-setter state preservation.
- Read back source, smoke and registration from current `main` and inspect the exact pushed commit diff.
- Close this claim with exact commit SHAs and truthful validation boundaries.

## Completion condition

Core opening physical-size setters fail closed on zero/negative values while sill offsets preserve existing semantics, regression evidence is committed on `main`, and this claim is marked `COMPLETED`.