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
## Follow-up — native Undo boundary after regeneration

Fresh licensed V25 rerun on merged/current `main` `21c7946447b1383efd53ee000d527b84acfc8969`
still reproduced `native_undo / UNDO_HOST_OWNERSHIP_REJECTED` with exact plugin/Core
identity and complete cleanup. A Core reproducer proved the remaining boundary defect:
when the persistence checkpoint is captured before `RegenerateDirty`, transition restore
fails because the target semantic state includes pre-regeneration quantities; capturing
the native-before checkpoint after regeneration makes the same transition pass.

This follow-up additionally reserves only the Undo-order hunk in
`src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs` and the matching ordering contract
in `scripts/preflight-curtain-undo-semantic-coherence.py`. The intended order is:
command rollback snapshot -> semantic regeneration -> selected-owner Undo capture/register
-> LINE/path host/frame/panel native mutation. The six partition guards owned historically
by #1106 are not changed. No Core hardening relaxation is permitted.

Completion requires TDD RED on the old capture-before-regeneration ordering, GREEN after
the minimal reorder, all existing source/Core/V25 gates, protected PR integration, and a
fresh exact-merged-main licensed synthetic lifecycle rerun. No `LOCAL_PASS` from source
or static evidence alone.

## Follow-up — composite pre-regeneration persistence target

Merged `main@6767a77ae439b45781f2289facb595c4964eee3a` advanced LOCAL-003 Level session one to PASS, but the embedded historical P11 contract reported `UNDO_NATIVE_REMOVED_SEMANTIC_NOT_RESTORED`. Source audit proved the P11 semantic signature includes pre-command project revision/time and owner Dirty plus generated-owner state, while the hardened `ProjectPersistenceCheckpoint` must validate post-regeneration quantities/properties before restoring persistence metadata.

This lane now additionally reserves the smallest composite-target extension in `ProjectPersistenceCheckpoint`, matching Core smoke coverage, and the existing Curtain Undo coordinator/build ordering hunks already owned by this claim. The target design preserves pre-regeneration project/element persistence stamps, binds the semantic signature only after deterministic regeneration, and still requires an exact current transition guard before restore. Normal `Restore()` and stale/replacement/newer-generation refusals remain unchanged.

P11 probe/runner files remain out of scope: the goal is to preserve the prior P11 product guarantee rather than weaken its automation expectation. Completion requires Core RED->GREEN, focused Curtain/LOCAL-003/generic gates, Core smoke, V25 build, protected CI merge, then fresh exact-merged-main licensed lifecycle evidence. No private/customer DWG and no broad LOCAL_PASS claim.
## Follow-up ? rebound registration admission

Fresh exact-merged-main licensed V25 evidence on `43399774d78b15567a255b1c876519c26ff39d10` reached the P11 baseline only to fail `baseline / STATE_REJECTED`; the guarded runner still restored the repository-synthetic DWG byte-for-byte and verified process/script/sidecar/lock/backup cleanup. The P11 probe and Curtain health code are unchanged from the earlier baseline-green `6767a77a...` generation.

Root-cause tracing found a deterministic product contradiction in the composite target: `ProjectPersistenceCheckpoint.RebindSemanticState()` intentionally keeps pre-command revision/Dirty/timestamp stamps while rebinding the post-regeneration semantic signature, so the rebound target is intentionally not a full `Matches(project)` source. `CurtainWallUndoCoordinator.BeginTransition()` nevertheless required that full match immediately after regeneration, causing registration rejection and atomic rollback before host/frame/panel geometry existed.

This follow-up reserves only the existing coordinator admission hunk and matching `preflight-curtain-undo-semantic-coherence.py` contract. Registration may validate `CoreMatches + SemanticMatches`; native Undo/Redo synchronization, transition-source guards, target restore and final target verification must continue to require full persistence `Matches()`. P11 runner/probe files remain unchanged. Completion still requires protected CI plus a fresh licensed synthetic lifecycle rerun; no `LOCAL_PASS` from source evidence alone.
