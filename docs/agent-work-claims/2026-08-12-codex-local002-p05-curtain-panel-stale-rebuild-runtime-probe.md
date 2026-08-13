# Work claim — LOCAL-002/P05 Curtain panel stale/rebuild runtime probe

- Status: `COMPLETED`
- Agent: `codex-local002-p05-curtain-panel-stale-rebuild-runtime-probe-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T13:02:00+07:00`
- Baseline main SHA: `5062ea60ec9fdf889f6b983fc1de91f3e496aebc`
- Priority: `LOCAL-002 / P0 / P05` — prepare one guarded exact-V25 cell for Curtain panel stale/Health transitions and coherent replacement.

## Readiness audit

P01-P04 have bounded licensed PASS evidence. Current source already provides the P05 production contracts: `ProjectElement.SetProperty(...)` marks output-affecting GlassWall grid/depth/height changes stale; `HostLinkService` marks linked-opening frame/panel output stale; line/path panel builders replace exact owned sets, stamp config/live fingerprints and clear panel stale only after a valid replacement; Core/live/runtime Health distinguishes explicit semantic stale, config drift, live CAD drift and native ownership failures. `QS3DSYNCSOURCE` explicitly reconciles direct CAD edits and invalidates owned generated output before rebuild.

No guarded runtime cell currently exercises these transitions in sequence while proving an unrelated GlassWall owner stays unchanged. Production source is sufficient and remains read-only.

## Reserved scope

- `src/QS3D.BricsCAD.V25/CurtainPanelStaleRebuildRuntimeProbeCommands.cs` — new automation-only seed/mutate/verify/probe commands.
- `scripts/test-bricscad-v25-curtain-panel-stale-rebuild.ps1` — new exact-SHA disposable-copy V25 runner.
- `scripts/preflight-curtain-panel-stale-rebuild-runtime-probe.py` — new focused source/privacy/cleanup/runtime-contract gate.
- `docs/CURTAIN-NATIVE-PANELS.md` — bounded P05 handoff only.
- this claim for close-out.

All production builders, planners, fingerprints, Health providers, ownership/invalidation services, `QS3DSYNCSOURCE`, HostLink, Level placement and existing P01-P04 probes/runners remain read-only. `docs/LOCAL-AGENT-INBOX.md` remains read-only because LOCAL-003 owns that shared surface.

## Synthetic P05 state machine

- Automation seeds two distinct ordinary 5 m legacy/no-Level LINE GlassWalls: one target and one unrelated control, plus one separate 1 m Door source geometrically inside the target but initially unlinked. Production `QS3DGLASSWALL` / `QS3DDOOR` perform semantic capture and production `QS3DCURTAIN3D` performs every native build.
- Baseline verification independently classifies target/control by source geometry; requires clean panel owner state, live owned host/frame/panel solids, canonical count/grid/depth/height/area/config/live fingerprints and source length; and snapshots only process-local owner/output evidence. No raw identity reaches a result marker.
- Grid mutation uses public instance `SetProperty` for max panel width/height. Before rebuild it requires owner panel stale, the canonical stale snapshot still matching the exact old panel set, Core `CURTAIN_PANEL_GENERATED_STALE`, live `CURTAIN_PANEL_CONFIG_STALE`, native ownership health remaining clean and the unrelated control unchanged. After target-only rebuild it requires stale cleared, clean Health, a new complete owned set/config fingerprint, independently planned grid/count/area/native AABBs and unchanged control evidence.
- Depth mutation changes target `ThicknessM`. The stale/Health requirements repeat. Rebuild must replace the target set, change config fingerprint and native depth, preserve the independently expected grid/count/clear area, and leave control unchanged.
- Height mutation changes target `HeightM`. Rebuild must replace the target set, change fingerprint/grid/count/area/native height coherently, clear stale/Health and leave control unchanged.
- Relationship mutation links the existing Door to the target with production `HostLinkService`. Before rebuild it requires owner stale plus Core/live config/live geometry stale and unchanged control. Rebuild must become canonical opening-aware output, change fingerprint/count/remaining area consistently with the independently reconstructed clipped plan, preserve positive pieces/no positive-area opening intersection, clear stale/Health and leave control unchanged.
- Direct CAD source edit extends only the target LINE from 5 m to 6 m. Before `QS3DSYNCSOURCE`, the probe must distinguish this from semantic owner mutation: owner stale flag/snapshot stays absent and the semantic length sample stays 5 m, while live Health reports geometry and config stale. Production `QS3DSYNCSOURCE` must then reconcile length to 6 m and ownership-safely invalidate/remove the old target output; target-only production `QS3DCURTAIN3D` rebuilds it. Final verification requires canonical clean output/fingerprints/count/area/native geometry for 6 m, unchanged opening relationship, and unchanged control.
- Every rebuild proves target old/new panel Handle sets are disjoint, all old target panels are no longer live, every new native panel uniquely matches one authoritative planned AABB, source/opening/host/frame/panel ownership is disjoint, one new panel Locates to the target owner and zero blocking Core/live/runtime panel Health remains.

This qualifies only one ordered synthetic P05 legacy/no-Level sequence. It does not qualify arbitrary edit ordering, P06-P12, Undo/save-reopen, exact curve behavior, Level placement or broad LOCAL-002 parity.

## Runner/privacy/cleanup boundary

- Require interactive Windows, nonblank initialized profile, clean exact Git SHA, exact repository x64 Release V25 DLL, guarded disposable suffix, empty outside-repository artifact directory, zero existing BricsCAD processes and no sidecar/backup.
- Launch one hidden PID, dismiss only its matching proxy-information dialog, stop only that PID, delete the private generated script, restore environment variables and verify zero remaining process/script/sidecar/backup plus unchanged disposable-DWG SHA-256 before PASS or sanitized FAIL publication.
- PASS is aggregate transition data only. No raw Handles, semantic IDs, project/profile names, paths, exception messages/types/stacks or drawing content. FAIL contains only an allowlisted phase and coarse rejection code after cleanup verification.

## Coordination and completion

- The active Curtain division tiny-ratio claim owns only `CurtainWallLayoutPlanner.DivisionCount(...)` plus its focused smoke; this ordinary-scale runner consumes layout read-only.
- LOCAL-003 owns Level placement/builders only for vertical consumption, its probes/gates and the shared inbox. P05 is explicit legacy/no-Level and does not edit those surfaces. Other active claims are unrelated Navigation/semantic-tag/test-fixture lanes.
- Merge this claim-only reservation before implementation and re-fetch active claims. Parse the runner; run focused P05/P04/P03/P02/native/stale/path/orchestration/runtime-health/Level/source-reconcile/command-wiring gates, aggregate preflight and installed-reference V25 `Release|x64` build without launching BricsCAD.
- Deliver by normal PR/squash merge without force-push or Actions, close this source-preparation claim and hand one clean exact merged-main SHA to the local licensed agent.
- No BricsCAD launch, GitHub Actions dispatch, private/customer fixture or private runtime artifact access is authorized in this source batch. P05 and overall LOCAL-002 remain `PENDING_LOCAL` until a fresh exact-SHA run returns the complete PASS/cleanup contract.

## Completion evidence

- Source-preparation commit `29cc105330f0169954b70ea8a7a1a05b674ab5dc` was merged by PR `#949` as `503592cdcc61955a28417dba97d91771dc2e227b`.
- The new focused P05 gate, P01-P04/native/orchestration/runtime-health/Level/source-reconcile gates and all 716 aggregate feature gates passed. The runner parsed successfully and `git diff --check` passed.
- A diagnostic installed-reference V25 `Release|x64` build compiled the new command and completed with no API/source errors. The strict build remained blocked only by eight unrelated current-main UI warnings promoted to errors in `QuantitySummaryWindow.xaml.cs` and `WorkspacePanel.FooterContext.cs`.
- No BricsCAD launch, GitHub Actions, private/customer fixture, runtime artifact, release or package operation occurred. This closes only source preparation; P05 and overall LOCAL-002 remain `PENDING_LOCAL` pending a separately claimed exact-SHA licensed run.
