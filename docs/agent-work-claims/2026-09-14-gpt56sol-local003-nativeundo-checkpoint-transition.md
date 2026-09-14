# Work claim — LOCAL-003 native Undo checkpoint transition

- Status: `ACTIVE`
- Agent: `gpt56sol-local003-nativeundo-fix-20260914`
- Parent: `#72` / `LOCAL-003_LEVEL_LIFECYCLE_ONLY`
- Baseline main: `e60baf7e04898c09c4816113b89fdecf188ff691`
- Trigger: licensed V25 diagnostic `4cfd577c73d1e74ce0aef0bf406a2cb13718001c`
  reproduced `native_undo / UNDO_HOST_OWNERSHIP_REJECTED`.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectPersistenceCheckpoint.cs`
- focused Core checkpoint transition regression coverage
- `src/QS3D.BricsCAD.V25/CurtainWallUndoCoordinator.cs`
- LOCAL-003 Level lifecycle diagnostic probe/gate already carried by `4cfd577c...`
- this claim and sanitized #72 handoff only

## Contract

Keep normal `ProjectPersistenceCheckpoint.Restore()` fail-closed for stale/newer
project revisions, replacement project/element generations and semantic drift.
Add only a bounded transition-restore path that requires an exact current
checkpoint for the same project generation and owner set before lowering the
persistence revision to a verified target checkpoint.
## Exclusions

No P11 runner takeover; `2026-08-14-codex-p11-undo-boundary-diagnostics.md`
retains its automation-only surfaces. No private/customer DWG, V26, release,
signing, installer or unrelated persistence refactor. Closed issue #987 is
historical context only; this successor is driven by #72 current evidence.

## TDD / completion

1. Add a Core RED regression reproducing before-checkpoint → committed newer
   checkpoint → semantic target restore → persistence transition rollback.
2. Preserve all existing stale/replacement/semantic-drift refusal tests.
3. Implement the smallest transition API and wire Curtain Undo to it.
4. Run focused gates, generic preflight, Core smoke and V25 Release build.
5. Push exact SHA, rerun licensed synthetic Level lifecycle, and publish only
   sanitized evidence. No `LOCAL_PASS` unless native evidence is green.