# Work claim — LOCAL-002 P09 Curtain post-commit warning isolation

- Status: `ACTIVE`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25)
- Registered: `2026-08-13T12:20:10.8843238+07:00`
- Baseline main SHA: `b34d8f1731c00934b72df1bad01b9c381a8b6809`
- Priority: `LOCAL-002 / P09 / P0` — prove that live-fingerprint and UI failures after the outer native commit never claim rollback or destroy committed Curtain output

## Reserved scope

Add one bounded automation-only, in-memory, one-shot post-commit failure seam to `QS3DCURTAIN3D` and qualify two independent cases on a repository-generated synthetic legacy/no-Level LINE GlassWall:

1. inject immediately after `commandTransaction.Commit()` and before frame/panel live-fingerprint stamping; production must report its `nativeCommitted=true` post-commit warning, retain the complete newly committed host/frame/panel set, remove the complete old set, and expose the missing live-fingerprint Health warnings instead of claiming rollback;
2. inject inside the best-effort `FinalizeUi` block before palette/viewport refresh; the failure must be swallowed by the existing UI warning boundary while the newly committed geometry, live fingerprints and zero-blocking-Health state remain valid.

The probe must use production `QS3DCURTAIN3D` for every replacement, independently verify one-shot ticket consumption, complete old/new ownership-set replacement, source preservation and the expected Health state, then publish aggregate-only evidence.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs` — two post-commit hook calls only, preserving the existing `nativeCommitted` and `FinalizeUi` warning paths;
- new `src/QS3D.BricsCAD.V25/Cad/CurtainWallPostCommitFailureInjection.cs` — internal thread-safe allowlisted one-shot seam with no environment-variable access or public command;
- new `src/QS3D.BricsCAD.V25/CurtainPanelPostCommitRuntimeProbeCommands.cs` — authenticated automation-only seed/arm/verify state machine;
- new `scripts/test-bricscad-v25-curtain-panel-postcommit-warnings.ps1` — exact-SHA disposable-copy runner;
- new `scripts/preflight-curtain-panel-postcommit-runtime-probe.py` — source/order/privacy/cleanup gate;
- `docs/CURTAIN-NATIVE-PANELS.md`, `docs/LOCAL-AGENT-INBOX.md` and this claim for truthful P09 handoff/evidence.

## Excluded scope

- No edits to host/frame/panel builders, planners, ownership/Health/Release/Locate/Level services, QSDB persistence or product UI implementation.
- No P10-P12 Health/Release selection matrix, Undo/save-reopen, multi-DWG, Family-editor or overall LOCAL-002 promotion.
- No external or persistent production fault switch: the seam remains internal, one-shot and armable only by the authenticated automation probe in the same assembly.
- No private/customer drawing, GitHub Actions, installer, signing or release work.

## Validation plan

- Merge this claim alone and verify it is reachable from current `origin/main` before source edits.
- Build exact candidate `Release|x64` against installed V25 references at zero warnings/errors; run P01-P09/native/orchestration/runtime-health/Level gates, runner parser, `scripts/preflight.py` and aggregate.
- Use a fresh repository-generated disposable copy, initialized profile, empty outside-repository artifact directory and zero pre-existing BricsCAD/sidecar/backup/drawing-lock state.
- Require exact old-set removal/new-set completeness for both post-commit cases; missing frame/panel live-fingerprint Health after the stamp injection; zero blocking panel Health after UI injection; unchanged source and DWG hash; verified process/script/drawing-lock/sidecar cleanup.
- Publish only counts, booleans, allowlisted failure phase/code, exact Git/DLL/BricsCAD identities and cleanup hashes—never raw Handles, semantic IDs, paths, profiles, drawing content or exception details.

## Coordination

Current ACTIVE/BLOCKED claims and open PRs were audited at registration. The active Curtain tiny-ratio claim owns only Core `CurtainWallLayoutPlanner.DivisionCount(...)` plus a focused smoke; LOCAL-003 owns configured-Level placement and explicitly leaves legacy/no-Level P09 independent. No claim owns the proposed post-commit seam, commands, runner, gate or exact P09 scenario.

## Completion condition

The seam/probe/runner/gate is merged, a clean exact-main licensed run proves both post-commit cases and cleanup, docs mark only P09 `LOCAL_PASS`, and this claim is `COMPLETED`. P10-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

## Source-preparation status

- The internal two-phase one-shot seam, post-commit orchestrator hooks, synthetic state-machine probe, guarded runner, static/privacy gate and P09 runbook handoff are implemented locally. No builder, planner, Health/Release/Locate/Level/QSDB/UI implementation changed.
- Runtime status remains `PENDING_LOCAL` until the source-preparation batch is merged and a fresh clean exact-main SHA/DLL proves both committed replacements plus cleanup.

## Runtime-discovered frame-fingerprint expansion

Exact clean candidate `e0cfce0c70bda041d535b7a9b7b51ed00dd4e971` reached the injected post-commit fingerprint verifier and failed closed at `VERIFY_FINGERPRINT / STATE_REJECTED` after cleanup. Source audit showed the LINE/path panel commits already remove `GeneratedCurtainPanelLiveFingerprint`, while the corresponding frame commits retain the prior `GeneratedCurtainFrameLiveFingerprint`. If stamping fails after replacement, Health can therefore inspect a stale frame fingerprint instead of the deterministic missing-fingerprint review state required by this P09 boundary.

This claim now additionally reserves exactly:

- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs` — remove `GeneratedCurtainFrameLiveFingerprint` inside the existing semantic commit before clearing frame stale state;
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs` — apply the same one-property removal for path frames;
- the existing P09 static gate and exact-SHA runtime regression needed to prove both missing-fingerprint warnings after the injected stamp failure.

This narrowly supersedes the earlier builder exclusion. It does not change frame geometry, Level placement, topology, counts, ownership, native transactions or Health rules. The BLOCKED LOCAL-003 claim belongs to the same `/root` local owner and reserves only Level-placement consumption in these builders; this P09 expansion preserves that merged placement implementation unchanged.
