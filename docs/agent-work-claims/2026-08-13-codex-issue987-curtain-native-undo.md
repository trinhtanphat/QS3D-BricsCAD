# Work claim — Curtain native Undo/Redo semantic coherence

- Status: `ACTIVE`
- Agent: `codex-issue987-curtain-native-undo-20260813` (`/root/fix_source_reconcile_undo`, continued by `/root/fix_curtain_undo`)
- Registered: `2026-08-13T17:05:00+07:00`
- Baseline main SHA: `8d819d51a25009d2b99eea2dda0a9e158baa8439`
- Priority: GitHub issue `#987` / `LOCAL-002 P11` production blocker reproduced on licensed BricsCAD V25

## Reserved scope

Fix the production `QS3DCURTAIN3D` command boundary so native Undo/Redo restores the canonical in-memory `ProjectState` owner metadata corresponding to the host/frame/panel CAD generation restored by BricsCAD. The bridge must remain document/project bound, preserve the existing six-phase outer CAD transaction and pre-commit `ProjectStateSnapshot` rollback, keep post-commit warnings non-transactional, and fail closed rather than applying a snapshot to a replaced project or another DWG.

The implementation will use a native transaction-bound Curtain revision marker plus in-session, document-scoped before/after semantic snapshots. Only unambiguous native Undo/Redo command completion may observe a restored known marker and restore the matching snapshot for the exact cached canonical project. Project reload/forget and document close discard stale history.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`
- new focused Curtain semantic/native Undo coordinator under `src/QS3D.BricsCAD.V25/`
- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs` only for exact cached-project identity and lifecycle cleanup
- focused deterministic/static regression coverage under `scripts/`
- `src/QS3D.Core/Persistence/ProjectPersistenceCheckpoint.cs` plus focused Core smoke registration, only for an exact, selected-element persistence stamp checkpoint that restores `ProjectState.ChangeVersion` / `UpdatedUtc` and selected-owner `Dirty` / `UpdatedUtc` without `Touch()` or public setter sequencing
- `docs/CURTAIN-NATIVE-PANELS.md`, `docs/LOCAL-AGENT-INBOX.md`, and this claim for the corrected exact-SHA handoff

## Excluded scope

- No edits to the local-owned P11 automation surfaces: `CurtainPanelUndoReopenRuntimeProbeCommands.cs`, `test-bricscad-v25-curtain-panel-undo-reopen.ps1`, or `preflight-curtain-panel-undo-reopen-runtime-probe.py`.
- No edits to any LOCAL-004 Source Reconcile probe, runner, gate, inbox contract or evidence.
- No BricsCAD runtime, private/customer DWG, GitHub Actions, release, installer, signing or V26 work.
- No Curtain geometry planning, clipping, source-shape support, ownership format, Health policy, P10 selection issue `#982`, P12 modeless/multi-DWG qualification, or broad global Undo framework.
- No whole-project semantic rollback during native Undo/Redo, no AuditTrail mutation, and no general-purpose transaction framework; the Core checkpoint is persistence-stamp-only and selected-element-bounded.

## Validation plan

- Add focused source/static coverage proving the Curtain marker is staged in the same outer native transaction only after semantic after-snapshot allocation, published history advances only after successful CAD commit, and command failure rollback remains unchanged.
- Cover consecutive builds, semantic-only intervening work/rebase, known Undo/Redo marker transitions, unknown revision refusal, exact cached-project/document affinity, lifecycle cleanup, and unambiguous command-name filtering.
- Deterministically prove exact persistence checkpoint restore at `long.MaxValue` without overflow/`Touch()`, exact selected-owner `Dirty`/timestamp restoration, project/element affinity refusal before mutation, unrelated element preservation, and no audit mutation.
- Run focused Curtain/Undo preflights, strict manual-CI policy, generic preflight, all discovered feature gates, Core smoke, and installed-reference V25 `Release|x64` compile without launching BricsCAD.
- Request the existing guarded P11 runner be rerun by its local owner on the exact merged source SHA.

## Coordination

The completed local claim `2026-08-13-codex-local-curtain-p11-undo-save-reopen.md` owns only the guarded licensed probe/runner/evidence and explicitly excludes production source repair. Its sanitized V25 result is the handoff for issue `#987`. P10 (`#982`), P12 and LOCAL-004 remain independent and untouched.

Concurrent claim `2026-08-13-1702-chatgpt-web-gpt56sol-curtain-undo-semantic-coherence.md` was published from the same baseline after this lane was already reserved and landed the initial `CurtainWallUndoCoordinator`/command/lifecycle/static-gate implementation. This lane adopts that current-main implementation instead of retaining its predecessor's duplicate coordinator. Review of the landed source proved the initial restore only copied selected generated properties/source handles and then called `project.Touch()`, while the authoritative P11 semantic signature also binds project `ChangeVersion`/`UpdatedUtc` and owner `Dirty`. The bounded continuation therefore owns only the exact persistence-state correction, strengthened gate/test, canonical handoff, and this original issue claim; it does not create a second Undo system.

## Source implementation record

- Scope expansion was published before Core/lifecycle/docs edits in PR `#1030`, merged as `8c1dc9b316ddaa19124aecd84d2a04495615f67e`.
- The bounded correction was implemented in `7e2468add900fe4c5412bb6177c1e2f4cee23b29`, synchronized with current `main` as branch head `1e20c056f4a81586986e5fe8dd000b4032e708a1`, and merged through PR `#1035` as `884f2059fa520439640d95c8c8782cf5f160cdfa`.
- `ProjectPersistenceCheckpoint` captures/restores only the project persistence revision and the explicit selected-owner `Dirty` / `UpdatedUtc` stamps. It resolves project/element affinity before mutation, restores exact internal state without `Touch()` or public setter sequencing, and never changes `AuditEvents` or unrelated elements.
- `CurtainWallUndoCoordinator` now includes that exact persistence checkpoint in its before/after signatures, refuses intervening persistence drift, refreshes the committed after-snapshot only after live fingerprinting, verifies exact target restore, and clears document history on explicit reload/forget as well as document destruction.
- Validation on the synchronized implementation head passed the focused Curtain semantic-coherence gate, full Core smoke (`ALL PASS`), installed-reference BricsCAD V25 `Release|x64` build (`0 warnings / 0 errors`), manual-CI policy and generic preflight. The aggregate feature-gate run exposed one unrelated current-main updater/V26 token drift after concurrent fixes; it is outside this claim and was not edited. No GitHub Actions or BricsCAD runtime was launched.
- Source is `SOURCE_READY / PENDING_LOCAL`. The existing additive P11 probe, runner and gate were not edited. Issue `#987` and this claim remain open/`ACTIVE` until the local owner passes the complete guarded runner against the final exact merged `main` SHA.

## Completion condition

The bounded source fix and regressions are merged to current `main`; the exact merged SHA is handed to the local P11 owner; issue `#987` and this claim remain open/`ACTIVE` until the complete guarded V25 P11 matrix passes native Undo, Redo, cold reopen, rebuild, affinity and cleanup. Source/static/build evidence alone cannot promote `LOCAL_PASS` or close the issue.
