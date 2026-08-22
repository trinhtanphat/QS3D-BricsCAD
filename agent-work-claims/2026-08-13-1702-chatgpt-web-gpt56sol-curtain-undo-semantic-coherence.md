# Work claim — Curtain semantic/native Undo coherence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-undo-semantic-coherence-20260813`
- Registered: `2026-08-13T17:02:00+07:00`
- Baseline main SHA: `8d819d51a25009d2b99eea2dda0a9e158baa8439`
- Priority: `P0 / issue #987 / LOCAL-002 P11 source blocker` — a licensed V25 reproduction proved that native `UNDO 1` after a successful `QS3DCURTAIN3D` build restores CAD host/frame/panel geometry without restoring the matching in-memory semantic generated-owner generation.

## Reserved scope

Implement a document/project-affine Curtain Undo/Redo bridge so the semantic generated-owner state tracked by `QS3DCURTAIN3D` follows the same native revision as the committed host/frame/panel replacement. The native revision marker must be staged in the same outer command transaction as Curtain geometry. The in-session semantic history must fail closed on unknown marker/project/backing-store drift and cleanly detach with document lifecycle.

Expected bounded source surfaces:

- `src/QS3D.BricsCAD.V25/CurtainWallUndoCoordinator.cs` (new)
- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs` (minimal registration/commit integration)
- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs` (attach/detach only)
- `scripts/preflight-curtain-undo-semantic-coherence.py` (new focused source regression)
- this claim file

The semantic snapshot scope is intentionally limited to Curtain build-owned generated metadata for the selected GlassWall owners, so later unrelated semantic edits are not silently reverted by native Undo. Post-commit live fingerprint warnings remain post-commit warnings; they must not be converted into pre-commit rollback failures.

## Excluded scope

- issue #982 / Workspace generated Curtain selection and its files
- Source Reconcile behavior or `SourceReconcileUndoCoordinator.cs`
- Curtain geometry algorithms, Health weakening, generated ownership marker format, source selection semantics
- local runtime probe implementation/evidence, V26, installer/release, GitHub Actions
- no native BricsCAD PASS claim without an exact-SHA licensed V25 rerun

## Validation plan

- Re-fetch current `main`, issue #987, current Curtain build orchestration and Source Reconcile Undo precedent before source write.
- Focused regression must require: a dedicated Curtain marker namespace; same-transaction marker staging; before/after Curtain-owner metadata capture; Undo/Redo command-end observer; cached-project/backing-store/document affinity guards; lifecycle attach/detach; and preservation of current pre-commit rollback/post-commit warning boundaries.
- Re-fetch exact pushed source/test after implementation and verify registration ancestry/intervening commits for overlap.
- Keep issue #987 and LOCAL-002 P11 pending until a licensed exact-SHA BricsCAD V25 rerun proves native + semantic Undo/Redo coherence.

## Coordination

Recent claim/commit search found no source claim or commit for issue #987 / Curtain semantic Undo. The prior P11 local worker only recorded the blocker and explicitly left the production fix remote. Current Workspace generated-selection work (#982) is a separate lane and is excluded here. Source Reconcile's already-landed semantic Undo bridge is read-only precedent, not shared ownership.

## Completion condition

The bounded source fix and deterministic source regression are on current `main`, exact files are re-fetched, this claim is closed `COMPLETED` with exact SHAs, and the local licensed rerun remains explicitly pending rather than being inferred from source inspection.