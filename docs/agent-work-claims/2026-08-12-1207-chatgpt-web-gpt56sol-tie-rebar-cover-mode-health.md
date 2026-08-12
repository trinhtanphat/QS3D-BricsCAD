# Work claim — Tie Rebar cover/mode health integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-cover-mode-health`
- Registered: `2026-08-12T12:07:00+07:00`
- Baseline main SHA: `e6f56ebe33a331ff4abaa1588566551752432296`
- Priority: P1 — writer-owned generated Tie Rebar cover/mode metadata must not bypass health validation.
- Task Key: `CORE-TIE-REBAR-COVER-MODE-HEALTH`

## Confirmed defect

`ColumnTieSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedTieRebarCoverM` using `double.ToString("R", CultureInfo.InvariantCulture)` and `GeneratedTieRebarMode` as the exact literal `ColumnRectangularTies`. `ColumnTieLayoutPlanner` requires cover to be finite and nonnegative. `GeneratedTieRebarHealthService` currently validates handles, count, diameter and actual spacing but never reads either cover or mode.

Consequently malformed generated metadata such as non-finite/negative cover or unsupported mode can pass Tie Rebar health without field-specific evidence. Writer-valid aliases such as `0.050` or padded/case-varied mode text are also not distinguishable from writer-owned serialization.

## Non-overlap check

Recent commit search found no Tie Rebar cover/mode health lane. Existing Tie Rebar handle canonicality owns only handle tokens; Beam Stirrup lanes own a different provider.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- one focused Core smoke regression for generated cover/mode health
- this claim file

Do not modify Column Tie planner/builder, existing handle/count/diameter/spacing validation, ownership/native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- Generated cover must be present, finite and >= 0 or emit `TIE_REBAR_GENERATED_COVER_INVALID` as Warning.
- After cover validity, raw text must equal round-trip invariant spelling or emit `TIE_REBAR_GENERATED_COVER_NON_CANONICAL` as Error.
- Generated mode must be present and normalize to `ColumnRectangularTies`; missing/unsupported text emits `TIE_REBAR_GENERATED_MODE_INVALID` as Warning.
- A recognized case/outer-whitespace alias emits `TIE_REBAR_GENERATED_MODE_NON_CANONICAL` as Error instead of invalid.
- Existing handle/count/diameter/spacing/category/stale behavior remains unchanged.

## Completion condition

Malformed and alias cover/mode metadata is fail-visible, focused smoke coverage pins invalid/noncanonical/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
