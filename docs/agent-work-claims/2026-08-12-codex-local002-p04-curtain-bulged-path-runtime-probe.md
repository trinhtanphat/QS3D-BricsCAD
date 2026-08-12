# Work claim — LOCAL-002/P04 Curtain bulged-path runtime probe

- Status: `ACTIVE`
- Agent: `codex-local002-p04-curtain-bulged-path-runtime-probe-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T12:40:00+07:00`
- Baseline main SHA: `1df800d9e3c997b5ddd4a94b6f960248815664ae`
- Priority: `LOCAL-002 / P0 / P04` — prepare one guarded licensed-runtime cell for bounded native panels on a bulged open WCS-XY POLYLINE after bounded P03 PASS.

## Readiness audit

P01-P03 now have bounded licensed PASS evidence. Current production source reads open +Z WCS-XY POLYLINE bulges through `CadPolylinePathReader`, tessellates each bulged segment with `BulgeArcTessellator` using effective `WallArcSagittaM`, maps authoritative panel rectangles onto the tessellated stations with `CurtainPathFramePlanner`, and creates ownership-protected native fragments through the same centered-box path helper proven by P02/P03. Sagitta, area and positive-float Health hardening are merged and must be consumed unchanged.

No guarded P04 runner currently proves the configured sagitta bound, tessellation/path-piece budgets and native fragment bounds across the curved approximation. Source/static review is not runtime evidence.

## Reserved scope

- `src/QS3D.BricsCAD.V25/CurtainPanelBulgedPathRuntimeProbeCommands.cs` — new automation-only synthetic seed/configure/prepare/probe commands.
- `scripts/test-bricscad-v25-curtain-panel-bulged-path.ps1` — new exact-SHA disposable-copy V25 runner.
- `scripts/preflight-curtain-panel-bulged-path-runtime-probe.py` — new static source/privacy/cleanup/runtime-contract gate.
- `docs/CURTAIN-NATIVE-PANELS.md` — bounded P04 handoff only.
- this claim for close-out.

All existing production builders, readers, tessellators, planners, Health/ownership services, Level placement, Direct Draw/capture commands and P01-P03 probes/runners remain read-only. `docs/LOCAL-AGENT-INBOX.md` remains read-only because LOCAL-003 owns that shared surface.

## Synthetic P04 contract

- An automation-only seed command creates one ordinary unsaved three-vertex open POLYLINE in WCS XY with +Z normal: `(0,0) -> (4 m,0) -> (7 m,0)`. Vertex 0 carries exact bulge `1`, producing a 180-degree radius-2 m source arc; vertex 1 is straight. It selects that source, then production `QS3DGLASSWALL` captures the canonical GlassWall.
- An automation-only configure/prepare command requires exactly that legacy/no-Level owner, mutation-safely sets effective project `WallArcSagittaM` to exact canonical `0.001` m and reselects only the canonical source. Production `QS3DCURTAIN3D` remains the sole host/frame/panel builder.
- The probe independently re-reads the raw vertices/bulges, derives the source circle/radius and computes the allowed chord angle/segment count from the configured sagitta plus production's documented 10-degree angle ceiling. It requires exactly the bounded expected curved tessellation count, one additional straight segment, finite points on the radius, and every curved chord's independently calculated sagitta `<= 0.001 m`.
- Reconstruct the production tessellated centerline and authoritative `CurtainWallDetailPlanner` + `CurtainPathFramePlanner` plan. Require positive panel fragments on more than one curved tessellation segment and on the straight segment; every planned piece must stay within the production per-element budget and every source/tessellation count within its published bound.
- Independently read every owned native panel `Solid3d.GeometricExtents`; uniquely match its XY/Z AABB to one authoritative tessellated path piece at the existing strict metric tolerance. Native, Handle, metadata and authoritative piece counts must agree.
- Require canonical `PathPanelSolids` / `OpenPolyline` / `Complete` / zero-opening metadata, exact persisted sagitta `0.001`, disjoint source/host/frame/panel ownership, one live host, positive live frames/panels, zero blocking Core/live/runtime panel Health, and Locate of one panel to one canonical GlassWall.
- This qualifies bounded tessellated straight prisms only. It must publish `tessellated_fragments_only=true` and `exact_swept_curve_qualified=false`; it never claims an exact swept curved glass surface, corner aesthetics, P05-P12 behavior or broad LOCAL-002 parity.

## Runner/privacy/cleanup boundary

- Require interactive Windows, nonblank initialized profile, clean exact Git SHA, exact repository x64 Release V25 DLL, guarded disposable suffix, empty outside-repository artifact directory, zero existing BricsCAD processes and no sidecar/backup.
- Launch one hidden PID, dismiss only its matching proxy-information dialog, stop only that PID, delete the private generated script, restore environment variables and verify zero remaining process/script/sidecar/backup plus unchanged disposable-DWG SHA-256 before PASS or sanitized FAIL publication.
- PASS output is aggregate-only. No raw Handles, semantic IDs, project/profile names, local paths, exception messages/types/stacks or drawing content. FAIL contains only an allowlisted phase and coarse rejection code after cleanup verification.

## Coordination and completion

- The only active neighboring Curtain claim owns `CurtainWallLayoutPlanner.DivisionCount(...)` and its smoke; this normal-scale runtime probe consumes that planner read-only.
- LOCAL-003 owns Level placement, builders only for vertical consumption, its own probe/gates and the shared inbox. This P04 case is explicit legacy/no-Level and does not edit those surfaces.
- Merge this claim-only reservation before implementation and re-fetch active claims. Parse the runner; run focused P04/P03/P02/native/path/orchestration/runtime-health/Level/command-wiring gates, aggregate preflight and installed-reference V25 `Release|x64` build without launching BricsCAD.
- Deliver through normal PR/squash merge without force-push or Actions, close this source-preparation claim and hand one clean exact merged-main SHA to the local licensed agent.
- No BricsCAD launch, GitHub Actions dispatch, private/customer fixture or private runtime artifact access is authorized in this source batch. P04 and overall LOCAL-002 remain `PENDING_LOCAL` until a fresh exact-SHA run returns the full PASS/cleanup contract.
