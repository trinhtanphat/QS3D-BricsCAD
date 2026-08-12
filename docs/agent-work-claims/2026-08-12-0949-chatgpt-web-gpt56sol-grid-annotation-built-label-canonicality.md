# Work claim — Generated Grid Annotation built-label canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-built-label-canonicality`
- Registered: `2026-08-12T09:49:00+07:00`
- Baseline main SHA: `3f0915076869f92244a0b5b384bf157d2ef097ee`
- Priority: P1 — generated Grid Annotation built-label snapshots must preserve the exact writer-owned normalized label.
- Task Key: `CORE-GRID-ANNOTATION-BUILT-LABEL-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` reads `GridLabel` through a helper that trims the source text, then persists that normalized value into `GeneratedGridAnnotationLabel`. `GeneratedGridAnnotationHealthService` currently reads both the current label and built-label snapshot through a trimming helper before comparison. A malformed persisted snapshot such as `" G1 "` can therefore appear equal to current canonical `"G1"` and avoid health evidence even though the writer never emits surrounding whitespace.

## Non-overlap check

The adjacent Grid Annotation owner canonicality lane is completed and explicitly excluded built-label health. Existing Grid Naming canonicality covers the current `GridLabel`, not the persisted `GeneratedGridAnnotationLabel` build snapshot. No recent claim/commit was found for built-label snapshot canonicality.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused Core smoke regression for built-label canonicality
- this claim file

Do not modify GridAnnotationBuilder, current GridLabel canonicality, owner tokens, handle parsing/count semantics, sizing metadata, native XData, persistence format or BricsCAD runtime code.

## Intended contract

- A built-label snapshot with surrounding whitespace emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing `GRID_ANNOTATION_LABEL_STALE` continues to compare normalized values, so a malformed snapshot for a genuinely different label still remains stale.
- Exact canonical snapshots preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Padded built-label snapshots are fail-visible, focused smoke coverage pins alias/stale/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
