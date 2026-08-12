# Work claim — Interchange provenance drawing scope

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T19:46:00+07:00`
- Baseline main SHA: `d251e87152eba0b96737253d94b133fd1d3e6cf9`
- Priority: evidence-driven remote-safe Core provenance hardening

## Reason

`ProjectInterchangeSourceHandleProvenance.Store()` is a public primitive that can persist drawing-local `sourceHandles` even when the source project has no `DrawingFingerprint`. `ProjectInterchangeJsonValidator` accepts that combination, while every provenance-aware import wrapper (Append, KeepTarget, UseSource) separately rejects it because drawing-local handles cannot be safely scoped to an unknown source drawing. Direct callers of the primitive therefore bypass the invariant enforced by all composed import paths and can create unscoped persisted provenance.

## Reserved scope

Centralize the existing provenance drawing-scope invariant in `ProjectInterchangeSourceHandleProvenance` so both `Plan()` and `Store()` fail closed when at least one source handle is present and the source project drawing fingerprint is blank. Preserve handle-free provenance behavior, record encoding, target CAD ownership semantics, wrapper behavior, and rollback semantics.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceDrawingScopeSmoke.cs`
- this claim file

## Excluded scope

- No changes to interchange JSON schema or generic validator acceptance rules.
- No changes to native cleanup authority, source-handle ownership, mapping semantics, UI, exporters, or BricsCAD V25 runtime.
- No GitHub Actions dispatch.

## Validation plan

- Assert `Plan()` rejects a valid interchange snapshot that contains source handles but has a blank project drawing fingerprint.
- Assert `Store()` rejects the same state without mutating the target project.
- Assert handle-free provenance remains permitted with a blank source drawing fingerprint.
- Re-fetch current `main` and target blobs before product/test writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` or licensed runtime run from this hosted session.

## Coordination

Recent provenance-handle integrity work is `COMPLETED`; no current claim was found for provenance drawing-scope enforcement. This lane does not overlap the just-completed QSDB drawing-fingerprint canonicality work because it is limited to interchange source-handle provenance storage.

## Completion condition

Current `main` enforces drawing scope at the provenance primitive boundary, includes focused CAD-independent regression coverage, and this claim is marked `COMPLETED`.
