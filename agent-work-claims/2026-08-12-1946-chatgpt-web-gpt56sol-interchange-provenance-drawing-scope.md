# Work claim — Interchange provenance drawing scope

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T19:46:00+07:00`
- Baseline main SHA: `d251e87152eba0b96737253d94b133fd1d3e6cf9`
- Priority: evidence-driven remote-safe Core provenance hardening

## Reason

`ProjectInterchangeSourceHandleProvenance.Store()` was a public primitive that could persist drawing-local `sourceHandles` even when the source project had no `DrawingFingerprint`. `ProjectInterchangeJsonValidator` accepts that combination, while the provenance-aware import wrappers separately reject it because drawing-local handles cannot be safely scoped to an unknown source drawing. Direct callers of the primitive could therefore bypass the invariant enforced by the composed import paths and create unscoped persisted provenance.

## Reserved scope

Centralize the existing provenance drawing-scope invariant in `ProjectInterchangeSourceHandleProvenance` so both `Plan()` and `Store()` fail closed when at least one source handle is present and the source project drawing fingerprint is blank. Preserve handle-free provenance behavior, record encoding, target CAD ownership semantics, wrapper behavior, and rollback semantics.

## Completed changes

- Product fix: `6d4395dc0de69a4793251c20de09472bd79fe661` (`fix(interchange): require drawing scope for source-handle provenance`).
- Regression: `60f0cd12c89873f3d2c2808965382dbb44d00675` (`test(interchange): cover provenance drawing scope`).
- `Plan()` and `Store()` now share the primitive drawing-scope guard before provenance mutation.
- Handle-free provenance with a blank source drawing fingerprint remains permitted.
- Regression covers `Plan()` rejection, `Store()` rejection without target mutation, and the handle-free compatibility path.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceDrawingScopeSmoke.cs`
- this claim file

## Excluded scope

- No changes to interchange JSON schema or generic validator acceptance rules.
- No changes to native cleanup authority, source-handle ownership, mapping semantics, UI, exporters, or BricsCAD V25 runtime.
- No GitHub Actions dispatch.

## Validation

- Re-fetched live `main` and the target provenance blob before the product write; the source blob was unchanged by concurrent agents.
- Re-fetched `main` after the regression write; `60f0cd12c89873f3d2c2808965382dbb44d00675` is a descendant of product fix `6d4395dc0de69a4793251c20de09472bd79fe661`.
- GitHub compare from the product fix to the regression head reported `ahead_by: 2`, `behind_by: 0`; the only intervening concurrent change was an unrelated semantic-sheet smoke.
- Read back both commit diffs and confirmed the product commit only changes the provenance primitive invariant and the regression commit only adds the focused smoke.
- Source/static verification only in this hosted session. No repository `dotnet` execution, GitHub Actions dispatch, or licensed BricsCAD V25 runtime qualification is claimed.

## Coordination

Recent provenance-handle integrity work is `COMPLETED`; this lane remained separate from the concurrent numeric SourceHandle identity and semantic-sheet lanes. No force-push was used.

## Completion condition

Satisfied: current `main` enforces drawing scope at the provenance primitive boundary, includes focused CAD-independent regression coverage, and this claim is `COMPLETED`.
