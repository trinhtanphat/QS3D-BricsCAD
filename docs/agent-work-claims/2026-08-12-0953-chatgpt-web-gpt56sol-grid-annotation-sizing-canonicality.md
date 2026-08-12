# Work claim — Generated Grid Annotation sizing canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-sizing-canonicality`
- Registered: `2026-08-12T09:53:00+07:00`
- Baseline main SHA: `9479e0ea6944e5f018431c7ec0634912a13aef8c`
- Priority: P1 — generated Grid Annotation sizing snapshots must preserve the exact writer-owned round-trip numeric spelling.
- Task Key: `CORE-GRID-ANNOTATION-SIZING-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` persists both `GridBubbleRadiusM` and `GridTextHeightM` with `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedGridAnnotationHealthService.ValidateSizing(...)` currently trims and parses the stored strings, so padded, trailing-zero or scientific aliases can pass sizing health even though the writer never emits those spellings for the same parsed value.

## Non-overlap check

The adjacent Grid Annotation owner and built-label canonicality lanes are completed. Existing sizing health checks only finite/positive values and text/radius ratio. No recent claim/commit was found for writer-owned canonical numeric spelling of these generated sizing snapshots.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused Core smoke regression for sizing snapshot canonicality
- this claim file

Do not modify `GridAnnotationBuilder`, native geometry, current semantic/family sizing inputs, handle/label/owner metadata, persistence format or BricsCAD runtime code.

## Intended contract

- Valid positive sizing snapshots whose raw strings differ from parsed `ToString("R", InvariantCulture)` emit dedicated `HealthSeverity.Error` canonicality diagnostics.
- Existing invalid/nonfinite/nonpositive diagnostics keep precedence.
- Existing text-height/radius ratio validation continues to use parsed numeric values.
- Exact writer-owned numeric strings preserve current behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Padded/trailing-zero/scientific numeric aliases are fail-visible, focused smoke coverage pins aliases plus invalid/ratio/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
