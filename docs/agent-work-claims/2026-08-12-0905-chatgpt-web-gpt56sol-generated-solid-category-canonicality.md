# Work claim — Generated Solid category canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-solid-category-canonicality`
- Registered: `2026-08-12T09:05:00+07:00`
- Baseline main SHA: `2f1e11adb8faab79214a36c763cdb171342f7b03`
- Priority: P1 — persisted Generated Solid category metadata must match the exact writer-owned enum token.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-SOLID-CATEGORY-CANONICALITY`

## Confirmed defect

`GeneratedGeometryService.CommitReplacement(...)` persists `GeneratedSolidCategory` with exact `category.ToString()`. `ModelHealthService.ValidateGeneratedGeometry(...)` currently uses case-insensitive `Enum.TryParse(rawCategory, true, ...)` and only reports a mismatch when the parsed enum differs from the semantic element category. Case-varied, padded, or numeric aliases that parse to the same category can therefore pass baseline health even though the writer never emits those spellings.

## Non-overlap check

The adjacent Generated Solid ownership canonicality lane is completed and covers only ownership version/project/element tokens. Recent template-layer category canonicality is a different persisted surface. No recent claim/commit was found for canonical spelling of `GeneratedSolidCategory` itself.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for `GeneratedSolidCategory` canonicality
- this claim file

Do not modify `GeneratedGeometryService`, native XData ownership, semantic element category mutation, builders, persistence format or BricsCAD runtime code.

## Intended contract

- Any parseable `GeneratedSolidCategory` token that does not exactly equal the writer-owned `ElementCategory.ToString()` spelling fails visible as a dedicated `HealthSeverity.Error` diagnostic.
- Existing missing/invalid and category-mismatch diagnostics remain intact.
- Exact canonical category metadata preserves current behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Case-varied, padded and numeric aliases are fail-visible, focused smoke coverage pins those aliases plus canonical control and mismatch preservation, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
