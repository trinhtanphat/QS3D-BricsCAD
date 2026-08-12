# Work claim — Generated Grid Annotation handle-list canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-handle-list-canonicality`
- Registered: `2026-08-12T09:59:00+07:00`
- Baseline main SHA: `2808d90412f298dee0e008a7806a7e898c360366`
- Priority: P1 — generated Grid Annotation handle-list metadata must preserve the writer-owned delimiter/spacing contract.
- Task Key: `CORE-GRID-ANNOTATION-HANDLE-LIST-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` persists `GeneratedGridAnnotationHandles` as `string.Join(";", generatedHandles)`, so writer-owned list tokens have no surrounding whitespace. `GeneratedGridAnnotationHealthService` currently splits the stored list and trims each token before validation, allowing malformed persisted text such as `"A; B;C;D;E;F"` to pass handle validation with no canonicality evidence.

## Non-overlap check

Existing Grid Annotation handle health already rejects empty tokens, duplicates, non-hex values, source-handle overlap and wrong count. Recent owner/built-label/sizing canonicality lanes are completed and cover different metadata. No dedicated persisted Grid Annotation handle-list spacing claim/commit was found.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused Core smoke regression for handle-list token spacing
- this claim file

Do not impose hex-letter casing, reorder handles, modify empty-token/count/ownership logic, `GridAnnotationBuilder`, native XData, persistence format or BricsCAD runtime code.

## Intended contract

- A non-empty handle token with leading/trailing whitespace emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing invalid/duplicate/count/source-overlap logic continues to operate on the trimmed handle token.
- Empty tokens retain existing `GRID_ANNOTATION_HANDLE_INVALID` precedence rather than receiving canonicality noise.
- Exact delimiter-only handle lists preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Padded handle tokens are fail-visible without changing hex-case/order semantics, focused smoke coverage pins padded/canonical/invalid/duplicate behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
