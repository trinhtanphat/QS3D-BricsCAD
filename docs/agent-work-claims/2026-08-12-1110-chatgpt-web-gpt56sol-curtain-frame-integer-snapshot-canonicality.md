# Work claim — Curtain Frame integer snapshot canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-integer-snapshot-canonicality`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `ad62c1648569c5ae792378bdaefc7325b3778f8e`
- Priority: P1 — generated Curtain Frame integer snapshots must preserve exact writer-owned invariant decimal spelling.
- Task Key: `CORE-CURTAIN-FRAME-INTEGER-SNAPSHOT-CANONICALITY`

## Confirmed defect

The line/path Curtain Frame writers persist generated integer metadata with `int.ToString(CultureInfo.InvariantCulture)`, including `GeneratedCurtainFrameCount`, `GeneratedCurtainFrameBaseCount`, `GeneratedCurtainFrameOpeningCount`, `GeneratedCurtainFrameColumns`, `GeneratedCurtainFrameRows`, and for path mode `GeneratedCurtainFramePathSegmentCount` / `GeneratedCurtainFrameMappedFrameCount`. `GeneratedCurtainFrameHealthService` currently accepts these values through `int.TryParse(...)` only, so alternate spellings such as padded, signed-positive or leading-zero text can pass health even though the writers never emit them.

## Non-overlap check

Recent claim/commit search found no Curtain Frame integer/count canonicality lane. Completed Curtain Frame handle, mode, source-kind and geometry-snapshot canonicality lanes own different metadata. Other active Curtain/Reporting/Revision/XLSX lanes do not reserve these generated integer slots.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for generated integer snapshot canonicality
- this claim file

Do not modify Curtain Frame builders, handles, geometry doubles, mode/source-kind/fingerprint metadata, count arithmetic, native ownership/XData, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- After an integer snapshot parses and passes its existing positive/nonnegative domain rule, its raw text must equal `value.ToString(CultureInfo.InvariantCulture)` or emit `CURTAIN_FRAME_INTEGER_METADATA_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing missing/invalid/range warnings retain precedence and invalid values do not receive canonicality noise.
- Existing count/grid/opening/path mismatch calculations continue to use parsed integer values.
- Exact writer-owned decimal strings preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Alternate raw spellings for generated integer snapshots are fail-visible without changing invalid/mismatch semantics, focused smoke coverage pins required/optional/path aliases plus invalid and canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
