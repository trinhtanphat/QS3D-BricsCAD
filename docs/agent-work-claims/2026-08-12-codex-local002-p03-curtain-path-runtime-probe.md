# Work claim — LOCAL-002/P03 Curtain straight-path runtime probe

- Status: `COMPLETED`
- Agent: `codex-local002-p03-curtain-path-runtime-probe-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T12:23:00+07:00`
- Baseline main SHA: `57445e364e3c8cd5bdf5322e87c5d8409e8bcbf7`
- Priority: `LOCAL-002 / P0 / P03` — prepare one guarded licensed-runtime cell for native open straight-segment POLYLINE Curtain panels after bounded P02 PASS.

## Readiness audit

P01 LINE/no-opening and P02 LINE/opening clipping have bounded licensed PASS evidence. Current source already dispatches an open WCS-XY POLYLINE GlassWall through `CurtainWallPathPanelSolidBuilder`, maps the authoritative rectangular panel plan across path stations with `CurtainPathFramePlanner`, creates native panel solids through the shared centered-box helper, persists independent path-panel ownership/metadata and participates in Core/live/runtime Health plus Locate. P03 is the next highest-value executable cell because it extends the now-proven native panel lifecycle across a real corner without entering the P04 bulge/sagitta boundary.

No existing guarded P03 runner reconstructs the authoritative path plan or independently measures native pieces on every segment. Static source review is not runtime evidence.

## Reserved scope

- `src/QS3D.BricsCAD.V25/CurtainPanelPathRuntimeProbeCommands.cs` — new automation-only, aggregate-only P03 prepare/probe commands.
- `scripts/test-bricscad-v25-curtain-panel-path.ps1` — new exact-SHA disposable-copy V25 runner.
- `scripts/preflight-curtain-panel-path-runtime-probe.py` — new static source/privacy/cleanup/runtime-contract gate.
- `docs/CURTAIN-NATIVE-PANELS.md` — bounded P03 handoff only.
- this claim for close-out.

All existing production builders, planners, Health services, ownership services, Level placement, Direct Draw commands and P01/P02 probes/runners remain read-only. `docs/LOCAL-AGENT-INBOX.md` also remains read-only because the active LOCAL-003 Level claim reserves that shared handoff surface.

## Synthetic P03 contract

- Seed exactly one ordinary legacy/no-Level GlassWall from an open, non-bulged, +Z-normal WCS-XY POLYLINE with three vertices and two axis-aligned segments: 4 m positive X followed by 3 m positive Y.
- Reselect only its canonical source, run production `QS3DCURTAIN3D`, and require canonical `PathPanelSolids`, `OpenPolyline`, `Complete`, zero-opening metadata.
- Re-read the source and reconstruct the same authoritative layout/path plan from public Core planners without changing production inputs. Require exactly two source path segments, 7 m total source length, positive finite plan pieces and at least one mapped output piece on each segment.
- Independently read every owned native panel `Solid3d.GeometricExtents`; uniquely match its XY/Z bounds to one authoritative path piece at the existing strict metric tolerance, including the rotated second segment and shared centered-box placement. Native count, Handle count, metadata count and authoritative path-piece count must agree.
- Require source/host/frame/panel ownership disjointness, one live host, positive live frame/panel sets, zero blocking Core/live/runtime panel Health issues, and Locate of one generated panel to exactly one canonical GlassWall owner.
- Do not claim corner join aesthetics, bulged/tessellated P04 behavior, openings, Level placement, rebuild/stale, failure injection, Undo/save-reopen or overall LOCAL-002 parity.

## Runner and evidence boundary

- Require Windows interactive session, nonblank initialized profile, clean exact Git SHA, exact repository x64 Release V25 DLL, guarded disposable suffix, empty outside-repository artifact directory, no pre-existing BricsCAD process and no pre-existing QSDB/backup.
- Launch only one PID hidden, dismiss only its matching proxy-information dialog, stop only that PID, delete the private generated script, restore environment variables and verify zero remaining process/script/sidecar/backup plus unchanged disposable-DWG SHA-256 before PASS or sanitized FAIL publication.
- PASS output may contain only aggregate counts/booleans, exact Git SHA, public BricsCAD/plugin hashes and timestamps. No raw Handles, semantic IDs, project/profile names, local paths, exception messages/types/stacks or drawing content.
- FAIL marker is limited to an allowlisted phase and coarse rejection code, and is reported only after cleanup verification. P03 and overall LOCAL-002 remain `PENDING_LOCAL` on every FAIL.

## Coordination

- Active Curtain division claim owns only `CurtainWallLayoutPlanner.DivisionCount(...)` plus its smoke; this probe consumes the planner read-only with normal-scale inputs.
- Active Curtain path-sagitta Health claim owns only `GeneratedCurtainPanelHealthService` sagitta validation plus its smoke; this P03 lane neither edits that provider nor qualifies P04 sagitta behavior. If its change lands before integration, the probe must consume the merged Health result without weakening it.
- Active LOCAL-003 owns vertical placement consumption and shared inbox docs; this P03 case is explicitly legacy/no-Level and edits no placement/builder/inbox surface.

## Validation and completion

- Merge this claim-only reservation before any implementation edit, then re-fetch current active claims.
- Parse the runner, run the new focused static gate plus Curtain native/orchestration/P01/P02/runtime-health/Level-Curtain and Direct Draw gates, aggregate preflight and installed-reference V25 `Release|x64` build without launching BricsCAD.
- Deliver through normal PR/squash merge without force-push or Actions, close this source-preparation claim, and hand one clean exact merged-main SHA to the licensed local agent.
- No BricsCAD launch, GitHub Actions dispatch, private/customer fixture or private runtime artifact access is authorized in this source-preparation batch.
- Mark only P03 bounded `LOCAL_PASS` after a fresh exact-SHA run satisfies the complete marker and cleanup contract. Until then P03 and overall LOCAL-002 remain `PENDING_LOCAL`.

## Close-out

- Claim-only reservation: PR `#864`, squash merge `7cf46f23d917725abf2d36c060ae1fc403dc25a8`.
- Source preparation: branch commit `7e64c30e3259f43f890ab9f8c96d623c93dcc4d8`, PR `#872`, squash merge `8f97b6503a1f29d6c0cd5c41858c00fc275d78d2`.
- Added only the new automation command, runner, static gate and P03 runbook handoff reserved above. Existing Curtain builders, Core planners/Health, Level placement, Direct Draw, P01/P02 and the shared local inbox were not modified.
- Focused P03/P02/native/orchestration/P01/runtime-health/path-frame/Level-Curtain/Direct Draw/command-wiring gates and the runner PowerShell parser passed after current-main integration. Aggregate `scripts/preflight.py` passed. The installed-reference V25 `Release|x64` adapter build succeeded with zero warnings/errors.
- No BricsCAD launch, GitHub Actions dispatch, private/customer fixture or private runtime artifact access occurred.
- This completes source preparation only. P03 and overall LOCAL-002 remain `PENDING_LOCAL` until one fresh exact merged-main SHA/DLL produces the complete sanitized V1 PASS marker and all process/script/sidecar/backup/DWG cleanup invariants.
