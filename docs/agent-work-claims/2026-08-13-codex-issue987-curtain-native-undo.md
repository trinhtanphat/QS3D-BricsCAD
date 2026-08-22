# Work claim — Curtain native Undo/Redo semantic coherence

- Status: `ACTIVE`
- Agent: `codex-issue987-curtain-native-undo-20260813` (`/root/fix_source_reconcile_undo`)
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
- `docs/CURTAIN-NATIVE-PANELS.md`, `docs/LOCAL-AGENT-INBOX.md`, and this claim for the corrected exact-SHA handoff

## Excluded scope

- No edits to the local-owned P11 automation surfaces: `CurtainPanelUndoReopenRuntimeProbeCommands.cs`, `test-bricscad-v25-curtain-panel-undo-reopen.ps1`, or `preflight-curtain-panel-undo-reopen-runtime-probe.py`.
- No edits to any LOCAL-004 Source Reconcile probe, runner, gate, inbox contract or evidence.
- No BricsCAD runtime, private/customer DWG, GitHub Actions, release, installer, signing or V26 work.
- No Curtain geometry planning, clipping, source-shape support, ownership format, Health policy, P10 selection issue `#982`, P12 modeless/multi-DWG qualification, or broad global Undo framework.

## Validation plan

- Add focused source/static coverage proving the Curtain marker is staged in the same outer native transaction only after semantic after-snapshot allocation, published history advances only after successful CAD commit, and command failure rollback remains unchanged.
- Cover consecutive builds, semantic-only intervening work/rebase, known Undo/Redo marker transitions, unknown revision refusal, exact cached-project/document affinity, lifecycle cleanup, and unambiguous command-name filtering.
- Run focused Curtain/Undo preflights, strict manual-CI policy, generic preflight, all discovered feature gates, Core smoke, and installed-reference V25 `Release|x64` compile without launching BricsCAD.
- Request the existing guarded P11 runner be rerun by its local owner on the exact merged source SHA.

## Coordination

The completed local claim `2026-08-13-codex-local-curtain-p11-undo-save-reopen.md` owns only the guarded licensed probe/runner/evidence and explicitly excludes production source repair. Its sanitized V25 result is the handoff for issue `#987`. P10 (`#982`), P12 and LOCAL-004 remain independent and untouched.

## Completion condition

The bounded source fix and regressions are merged to current `main`; the exact merged SHA is handed to the local P11 owner; issue `#987` and this claim remain open/`ACTIVE` until the complete guarded V25 P11 matrix passes native Undo, Redo, cold reopen, rebuild, affinity and cleanup. Source/static/build evidence alone cannot promote `LOCAL_PASS` or close the issue.
