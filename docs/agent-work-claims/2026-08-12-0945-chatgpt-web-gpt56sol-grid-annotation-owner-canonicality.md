# Work claim — Generated Grid Annotation owner canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-owner-canonicality`
- Registered: `2026-08-12T09:45:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: P1 — generated Grid Annotation ownership metadata must match exact writer-owned semantic identities.
- Task Key: `CORE-GRID-ANNOTATION-OWNER-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` writes exact `GeneratedGridAnnotationOwnerProjectId = project.ProjectId`, `GeneratedGridAnnotationOwnerElementId = element.Id`, and `GeneratedGridAnnotationOwnershipVersion = "1"`. `GeneratedGridAnnotationHealthService.ValidateOwner(...)` currently reads all three values through a helper that trims before comparison and compares project/element ids case-insensitively. Padded/case-varied aliases can therefore pass ownership health even though the writer never emits those spellings.

## Non-overlap check

Existing Grid Annotation lanes cover null entries, empty handle tokens, canonical target binding, revision/audit lifecycle, and handle ownership. No recent claim/commit was found for canonical spelling of the three semantic Grid Annotation owner tokens. Built-label snapshot canonicality is explicitly excluded for a separate audit.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused Core smoke regression for owner token canonicality
- this claim file

Do not modify GridAnnotationBuilder, handle parsing/count semantics, built-label health, sizing metadata, native XData, persistence format or BricsCAD runtime code.

## Intended contract

- Padded/case-varied owner project and owner element ids fail visible as dedicated `HealthSeverity.Error` canonicality diagnostics when they normalize to the correct target.
- Padded ownership version fails visible.
- Existing version/project/element mismatch diagnostics continue to run against normalized values, so malformed spelling cannot hide an actual mismatch.
- Exact writer-owned values preserve current behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Non-canonical owner tokens are fail-visible, focused smoke coverage pins padded/case aliases plus mismatch preservation and canonical control, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
