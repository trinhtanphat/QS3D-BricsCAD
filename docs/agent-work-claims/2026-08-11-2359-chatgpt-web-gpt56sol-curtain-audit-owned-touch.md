# Work claim — Curtain generated audit-owned Touch

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:59:00+07:00`
- Baseline main SHA: `64f375afa565a86e73bc0dd1cdfade2b642b9d59`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

Four existing native Curtain generated-output builders already record one dedicated `AuditTrail` mutation per successfully updated GlassWall element, and `AuditTrail.Record(...)` owns `ProjectState.Touch()`. Each batch nevertheless performs an additional `if (pending.Count > 0) project.Touch();` immediately after its audited semantic-update loop and before the CAD transaction commit. A successful N-element Curtain batch therefore advances `ChangeVersion` N+1 times instead of exactly once per audited semantic mutation, creating avoidable freshness churn.

## Reserved scope

Remove only the redundant batch-level explicit Touch from:

- `CurtainWallFrameSolidBuilder`
- `CurtainWallPanelSolidBuilder`
- `CurtainWallPathFrameSolidBuilder`
- `CurtainWallPathPanelSolidBuilder`

Preserve all per-element audit records, frame/panel/opening/path planning, native ownership/XData, stale/fingerprint metadata, rollback, document lock, transaction ordering and editor/runtime behavior. Add one auto-discovered static preflight guarding the shared audit-owned revision invariant.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathPanelSolidBuilder.cs`
- `scripts/preflight-curtain-audit-owned-touch.py`
- this claim file

## Excluded scope

- No changes to Curtain geometry/layout formulas, opening interruption, panel/frame dimensions, path tessellation/sagitta, generated ownership, liveness checks, source binding, UI, commands, or project identity UI.
- No overlap with the active `CurtainWallWindow.xaml.cs` exact-project-identity claim.
- No changes to `AuditTrail` semantics.
- No GitHub Actions dispatch or release workflow.
- No claim of licensed BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch current `main` and all four exact target blob SHAs after claim registration; never force-push.
- Preserve each semantic commit helper and its existing dedicated audit action:
  - `geometry.curtain.frames`
  - `geometry.curtain.panels`
  - `geometry.curtain.path.frames`
  - `geometry.curtain.path.panels`
- Remove only the explicit batch-level `project.Touch()` following the audited update loop.
- Preserve `ProjectStateSnapshot` rollback, native transaction, audited update before CAD commit, generated ownership and existing mutation ordering.
- Add static preflight covering all four builders and rejecting reintroduction of explicit Build-level `project.Touch()`.
- Record source/static verification only; exact V25 behavior remains LOCAL_ONLY.

## Coordination

The current Curtain UI project-identity claim reserves only `CurtainWallWindow.xaml.cs` plus its preflight and does not overlap these CAD builders. Recent Curtain frame finite-bounds/opening-overflow work is completed. No current claim or recent commit was found reserving Curtain generated-output audit-owned revision semantics.

## Completion condition

All four Curtain generated-output builders advance project revision only through their existing per-element audit records, retain their native/rollback/geometry contracts, include a shared static regression gate, are merged to `main`, and this claim is marked `COMPLETED`.