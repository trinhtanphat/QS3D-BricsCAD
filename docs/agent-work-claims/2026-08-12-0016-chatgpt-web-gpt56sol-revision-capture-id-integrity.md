# Work claim — Revision capture snapshot ID integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-capture-id-integrity`
- Registered: `2026-08-12T00:16:00+07:00`
- Completed: `2026-08-12T00:18:00+07:00`
- Baseline main SHA: `bc550049b4fc82268761d9feef4d3f47f4e55673`
- Reservation commit: `3656d0cb104859579985ab442abf6d3695ac9748`
- Priority: P1 — RevisionService.Capture must not return a snapshot rejected by the revision persistence contract solely because of its requested revision ID.

## Defect fixed

`RevisionService.Capture(ProjectState, string revisionId)` previously stored `revisionId ?? string.Empty` without validation. The public method therefore accepted `null`, blank, and leading/trailing-whitespace IDs and returned a `RevisionSnapshot` that `RevisionSnapshotStore.Save(...)` later rejected because persisted revision IDs are required and canonical.

Capture now rejects invalid revision IDs before iterating project elements and preserves a valid canonical revision ID exactly.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionCaptureIdIntegritySmoke.cs`
- this claim file

## Published commits

- `1a46aca8d70db0730c6ae23ae2449212fd6d063f` — reject null, blank, and padded revision IDs at the public capture boundary.
- `1df2a135704f274d7c8dc796e537763bc66f2e2e` — add isolated auto-registered smoke covering invalid IDs, exact valid-ID preservation, and UTC capture timestamp kind.

## Delivered contract

- `Capture(...)` cannot return a snapshot whose revision ID is invalid solely because of the requested ID parameter.
- Canonical IDs are not trimmed or otherwise rewritten.
- Element capture, dependencies-as-semantic-set behavior, quantities, source handles, compare, persistence schema, and native/UI behavior are unchanged.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to reserved surfaces.
- Existing dependency canonicalization was deliberately preserved because the repository's dependency freshness regression defines dependencies as a semantic set.
- Dedicated smoke auto-registers via `ModuleInitializer`; no shared test registry was edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
