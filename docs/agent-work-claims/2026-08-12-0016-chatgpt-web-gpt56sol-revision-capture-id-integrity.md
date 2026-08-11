# Work claim — Revision capture snapshot ID integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-capture-id-integrity`
- Registered: `2026-08-12T00:16:00+07:00`
- Baseline main SHA: `bc550049b4fc82268761d9feef4d3f47f4e55673`
- Priority: P1 — RevisionService.Capture must not return a snapshot rejected by the revision persistence contract solely because of its requested revision ID.

## Confirmed defect

`RevisionService.Capture(ProjectState, string revisionId)` currently stores `revisionId ?? string.Empty` without validation. The public method therefore accepts `null`, blank, and leading/trailing-whitespace IDs and returns a `RevisionSnapshot` that `RevisionSnapshotStore.Save(...)` later rejects because persisted revision IDs are required and canonical.

This is the same boundary principle already applied to duplicate semantic Element IDs: capture should fail before returning a revision snapshot that violates invariants enforced by compare/store consumers.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `tests/QS3D.Core.SmokeTests/RevisionCaptureIdIntegritySmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- `Capture(...)` rejects null, blank, and padded revision IDs with `ArgumentException`.
- A canonical non-empty revision ID is preserved exactly.
- Element capture, dependencies-as-semantic-set behavior, quantities, source handles, compare, persistence schema, and native/UI behavior remain unchanged.

## Coordination

Earlier completed revision claims cover duplicate/canonical Element IDs, semantic reference IDs, payload invariants, dependencies, and persistence validation. None reserves the requested revision snapshot ID parameter. Dependency set canonicalization is intentionally preserved per its existing freshness regression.

## Validation plan

- Add a dedicated auto-registered smoke covering null, whitespace-only, leading/trailing whitespace, and a valid canonical ID.
- Re-fetch the source immediately before update, use the current blob SHA, inspect exact published diffs, and close this claim with exact SHAs.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Revision capture can no longer produce a snapshot with an invalid revision ID, focused regression is on `main`, and this claim is closed.
