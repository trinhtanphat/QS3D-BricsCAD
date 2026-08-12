# Work claim — Semantic Tag placement snapshot canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-placement-canonicality`
- Registered: `2026-08-12T12:36:00+07:00`
- Baseline main SHA: `65a61d91a5ffbeb4bbc426b367c9cf1d255e16fa`
- Priority: P1 — writer-owned Semantic Tag placement snapshots must preserve exact serialization.
- Task Key: `CORE-SEMANTIC-TAG-PLACEMENT-CANONICALITY`

## Confirmed defect

`SemanticTagBuilder.Build(...)` persists `GeneratedSemanticTagTextHeightM` and `GeneratedSemanticTagPositionX/Y/Z` with `double.ToString("R", CultureInfo.InvariantCulture)`, and persists `GeneratedSemanticTagPositionScope` as the exact `DrawingLocalWcs` token. `GeneratedSemanticTagHealthService` validates the numeric values only after trimming and validates scope through a trimmed property helper. Alternate writer-impossible spellings such as `0.180`, `0.0`, or padded ` DrawingLocalWcs ` can therefore pass health.

## Non-overlap check

Recent commit searches found no Semantic Tag position or text-height canonicality lane. The completed rotation-health lane owns only `GeneratedSemanticTagRotationRad`. Owner/template/text metadata remains explicitly out of scope.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- one focused Core smoke regression for text-height, scope and X/Y/Z canonicality
- this claim file

Do not modify rotation validation, owner/template/text semantics, Semantic Tag builder/runtime health, generated handle ownership, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- A finite positive text height must use exact round-trip invariant spelling or emit `SEMANTIC_TAG_TEXT_HEIGHT_NON_CANONICAL` as Error.
- A finite X/Y/Z position component must use exact round-trip invariant spelling or emit `SEMANTIC_TAG_POSITION_NON_CANONICAL` as Error.
- Scope classification remains backward compatible: values whose trimmed text is not exactly `DrawingLocalWcs` remain `SEMANTIC_TAG_POSITION_SCOPE_INVALID`; a padded exact token emits `SEMANTIC_TAG_POSITION_SCOPE_NON_CANONICAL` as Error.
- Existing invalid/nonfinite/nonpositive diagnostics retain precedence and do not receive canonicality noise.
- Exact writer-owned placement metadata preserves existing behavior.

## Completion condition

Representative text-height/position/scope aliases are fail-visible without changing invalid precedence, focused smoke coverage pins aliases plus invalid/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
