# Work claim — Semantic Tag PICKFIRST preflight sync

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T13:49:00+07:00`
- Baseline main SHA: `2857b2c5d5a81aa97d6e631f6dac919c9a01a746`
- Priority: `P0 source/static CI regression — semantic-tag lifecycle guard still expects Modal-only command flags after PICKFIRST support landed`

## Reserved scope

Reconcile the existing Semantic Tag static preflight with the already-implemented PICKFIRST command registration contract. The guard must require `CommandFlags.Modal | CommandFlags.UsePickSet` for `QS3DTAG`, `QS3DTAGREFRESH`, and `QS3DTAGREMOVE` instead of rejecting that intentional registration.

## Expected surfaces

- `scripts/preflight-semantic-tags.py`
- Read-only verification of `src/QS3D.BricsCAD.V25/SemanticTagCommands.cs`
- Read-only verification of `src/QS3D.BricsCAD.V25/SemanticTagRemovalCommands.cs`

## Excluded scope

- Semantic Tag product behavior, ownership, rendering, placement, removal, health, or runtime implementation.
- Semantic `SourceHandles` numeric-identity work and any currently reserved handle-identity lane.
- BricsCAD runtime qualification, command execution, UI/DPI work, GitHub Actions dispatch, release publication.
- Other QSDB/documentation/preflight failures outside this exact Semantic Tag command-flag mismatch.

## Validation plan

- Re-read the three current command registrations and prove all intentionally include `UsePickSet`.
- Update only the stale static expectations in `preflight-semantic-tags.py`.
- Read back the pushed script and confirm all three lifecycle registrations are guarded while unrelated lifecycle/rollback/health checks remain unchanged.
- Do not claim executable preflight, build, Actions, or BricsCAD runtime PASS unless actually executed.

## Coordination

Recent active claims reserve Formula arithmetic underflow, Grid Annotation handle identity, Preview Review CDATA shape, interchange name canonicality, Room Finish XLSX round-trip, Curtain Frame/Wall Mesh handle identity, quantity-rule dirty propagation and semantic SourceHandle identity. This reservation does not modify those surfaces. Existing Semantic Tag runtime/local qualification remains separate.

## Completion condition

A pushed `main` commit updates only the stale Semantic Tag command-flag preflight expectations, followed by this claim marked `COMPLETED` with the implementation SHA and validation actually performed.
