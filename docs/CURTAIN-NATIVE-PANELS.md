# Curtain native panels — source contract and local V25 matrix

Updated: 2026-08-11 (UTC+7)

## Status boundary

This document defines the clean-room QS3D contract for panel-by-panel native glass inside the BricsCAD-hosted Curtain workflow.

The repository source/static gate may prove that the ownership, planning, rollback, health and command wiring are present. It cannot prove BricsCAD `Solid3d` geometry, nested-transaction behavior, opening clearance, selection/Locate, Undo, or save/reopen behavior. Each row remains `LOCAL-002 / PENDING_LOCAL` until a licensed BricsCAD V25 run records sanitized evidence against one clean exact SHA. P01 through P04 now have bounded runtime results; they do not promote the remaining matrix or overall LOCAL-002 status.

Do not report `LOCAL_PASS`, production qualification, or BLT parity from a Core smoke test or Python preflight.

## Native output layers

One semantic `GlassWall` remains the owner of three distinct native layers:

1. `GeneratedSolidHandle` — the single backing GlassWall host used by the existing Door/Opening boolean lifecycle;
2. `GeneratedCurtainFrameHandles` — perimeter, mullion and transom overlay solids;
3. `GeneratedCurtainPanelHandles` — panel-by-panel clear-glass solids.

Panel output must not replace the backing host or reuse the frame owner slot. The panel slot participates in the canonical generated-owner policy, semantic selection/Locate, generated invalidation, Model Health and Release Readiness. A property name matching the slot is insufficient by itself: destructive replacement also requires the dedicated native ownership marker to agree with project, element and canonical owner slot.

## Planning and geometry contract

The panel builder consumes `CurtainWallDetailPlanner.Panels`; it does not create a second Curtain grid engine.

Supported source forms match the guarded Curtain workflow:

- a horizontal plan-view `LINE` GlassWall;
- an open WCS-XY `POLYLINE` with +Z normal, including bounded bulge tessellation.

Closed, tilted, malformed, degenerate and unsupported freeform paths fail closed. A path panel crossing a tessellated segment boundary is split into deterministic station-mapped fragments; this is bounded piecewise-linear output, not a claim of an exact swept curved panel.

Linked Door/WallOpening rectangles clip the clear panel cells before native placement. A partial overlap emits only the remaining positive-area fragments. If opening clipping removes every panel piece, the builder must either record an explicit complete zero-piece output state or fail closed before replacing the previous output; an empty handle string must never become indistinguishable from “never built”. Missing, ambiguous, off-host or invalid opening provenance fails closed rather than allowing glass through the opening.

For each selected GlassWall, the source planner and adapter enforce panel/fragment budgets and validate that element's complete old output before its panels are erased or replacements are appended. The outer command transaction is the whole-selection safety boundary: a later element failure must roll back every earlier element's native and semantic work.

## Replacement and rollback contract

Before destructive replacement, the adapter must validate the complete old handle set:

- canonical handles are unique;
- every expected handle resolves to a live `Solid3d`;
- every entity has the matching panel ownership marker;
- the project-wide owner index has no duplicate or foreign claim;
- the selected semantic element and source geometry are still canonical for the active DWG.

Any missing, stale, malformed, duplicate, foreign or ambiguously-owned item refuses replacement before erase.

`QS3DCURTAIN3D` must run LINE host, path host, LINE frame, path frame, LINE panel and path panel builders inside the same outer native transaction. It captures a `ProjectStateSnapshot` before semantic regeneration. A failure before the outer commit aborts all native phases and restores semantic ownership/metadata; it must not leave a half-updated host/frame/panel set. Post-commit fingerprint or UI refresh failure is a warning and must not claim rollback of already-committed geometry.

## Metadata, stale and health contract

Panel output records a dedicated `GeneratedCurtainPanel*` metadata family, including at minimum:

- canonical handles, an explicit completed-build marker and native fragment count;
- source panel-cell count and opening count;
- grid columns/rows;
- panel depth, source length and height;
- LINE/path mode and path-specific source/mapping counts where applicable;
- deterministic configuration fingerprint;
- live geometry fingerprint;
- independent generated-panel stale snapshot/state.

Changes to panel/grid/depth/placement configuration and linked-opening relationships must stale panel output without pretending the frame or backing host was rebuilt. Successful panel replacement clears only panel stale state. Invalidation removes the complete panel metadata prefix only after ownership-safe native cleanup.

Panel health must fail or warn deterministically for missing live solids, handle/count/grid/mode/path inconsistencies, stale configuration/live fingerprints, stale state and duplicate ownership. `QS3DHEALTHALL` and `QS3DRELEASECHECK` include panel health rather than treating panel solids as an untracked visualization layer.

## Static evidence

`scripts/preflight-curtain-native-panels.py` is the focused static source gate. It must cover the actual Core/adapter filenames and exact tokens of the implementation, including:

- Core panel clipping/mapping plan and bounds;
- panel owner slot, independent stale state and health codes;
- LINE/path native builders, dedicated XData marker and exact-set prevalidation;
- all six ordered `QS3DCURTAIN3D` native phases under one outer transaction;
- invalidation, selection/Locate, Health All and Release Readiness wiring;
- deterministic smoke registration for LINE/path/opening/ownership/stale cases.

A static PASS means only that these source contracts are present.

## Exact local evidence matrix

Run on one clean, final merged SHA and one DLL built from that exact SHA. Record the Windows and BricsCAD V25 build, SHA, DLL hash, disposable fixture identity/hash, commands, exit/result codes and local artifact directory. Keep raw handles, project IDs, drawing paths and private drawing content out of committed summaries.

| Case | Minimum scenario | Required result/evidence | Current status |
|---|---|---|---|
| P01 | LINE GlassWall, multiple grid cells, no opening | backing host + frame + panel layers are distinct; panel count/geometry/ownership agree | LOCAL_PASS at `3da7b20013233a71eb174c77e87d4618b370ebd4` (bounded basic case only) |
| P02 | LINE with Door/WallOpening fully and partially intersecting cells | no panel crosses the opening; positive fragments only; full-cover records the existing explicit healthy complete-empty state | LOCAL_PASS at `7b4a379da15c8c0bed60536bc0ccca7334eb4712` (bounded P02 only) |
| P03 | open straight-segment POLYLINE | station-mapped panel fragments follow every path segment; owner resolves to one GlassWall | LOCAL_PASS at `83b3f93274a60e8de3744cb8ae668ca7de381e5b` (bounded P03 only) |
| P04 | open bulged WCS-XY POLYLINE | bounded tessellated fragments follow the configured sagitta contract; no unbounded growth | LOCAL_PASS at `40502704b402b1aa55300f7f187b4fabd355eb40` (bounded tessellated P04 only) |
| P05 | grid, depth, height and linked-opening change followed by rebuild | panel stale state appears before rebuild and clears only after valid replacement | PENDING_LOCAL |
| P06 | one missing old handle, duplicate canonical handle, foreign/unmarked solid and cross-owner conflict | every case refuses before erase/append; surviving old set and semantic metadata remain unchanged | PENDING_LOCAL |
| P07 | panel/fragment budget exceeded and malformed/off-host opening provenance, including failure on a later selected element | fail closed before mutation of the invalid element; the outer transaction rolls back the whole batch so every previous valid output remains intact | PENDING_LOCAL |
| P08 | injected failure at semantic regeneration and each of six host/frame/panel phases | pre-commit failure leaves native and semantic snapshots unchanged; no partial phase commit | PENDING_LOCAL |
| P09 | injected live-fingerprint/UI failure after outer commit | committed geometry remains; truthful warning directs Health/Release review | PENDING_LOCAL |
| P10 | select a generated panel, Locate/Family review, Health All and Release Check | canonical GlassWall owner is resolved and panel health participates in both aggregate checks | PENDING_LOCAL |
| P11 | Undo, save/reopen and rebuild | native counts/ownership/fingerprints remain coherent; no foreign deletion | PENDING_LOCAL |
| P12 | two open DWGs with modeless Curtain Hub | command and refresh remain bound to the intended active DWG/project | PENDING_LOCAL |

For every case record before/after aggregate native counts, semantic `ChangeVersion`/stale state, result code and relevant health-code set. Screenshots may remain local; if a sanitized summary is committed, it must be tied to the exact SHA and must not contain customer content.

### P02 licensed-runtime handoff

`QS3DCURTAINOPENINGPROBE` and `scripts/test-bricscad-v25-curtain-panel-openings.ps1` prepare a bounded `LOCAL-002 / P02` run on two synthetic legacy/no-Level LINE GlassWalls in an ordinary fresh disposable copy. The partial Door case must remove at least one complete source cell, clip at least one other cell, emit only finite positive fragments, match every native extent uniquely to the authoritative Core plan, keep native/opening positive-area intersection at zero, and agree with panel count/area metadata. The complete-empty WallOpening case must fully remove every source cell while retaining `Complete` build state, opening-aware metadata, zero count/handles/area, and non-blocking Core/live/runtime panel Health. The non-empty case must also resolve one generated panel through Locate to exactly one canonical GlassWall; all source/opening/host/frame/panel ownership sets must be disjoint.

Run `scripts/preflight-curtain-panel-opening-runtime-probe.py` before the licensed test. The PowerShell runner requires the exact repository x64 Release V25 DLL from a clean exact SHA, a nonblank initialized profile, an empty artifact directory outside the repository, no pre-existing BricsCAD process/sidecar/backup, and the exact `*.curtain-opening-probe-copy.dwg` suffix. It stops only its launched PID, deletes its private script, restores environment variables, proves process/sidecar cleanup and unchanged disposable-DWG SHA-256, and publishes aggregate-only metadata without handles, IDs, paths or customer content. This guarded boundary remains the required contract for any later P02 requalification; the successful bounded evidence is recorded below.

The first licensed attempt at clean exact SHA `af0aec7f` reached the probe marker in about eight seconds but returned the original coarse `CURTAIN_PANEL_OPENING_RUNTIME_FAILED` code before any P02 aggregate could be accepted. The disposable DWG hash remained unchanged, the launched process exited, no sidecar remained and the private script was removed. That result is a diagnostic FAIL, not evidence of a production clipping defect: the old marker could not separate prior `QS3DCURTAIN3D` output absence/rollback from source/plan/metadata/native/Health/Locate validation. Marker schema `QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V2` now reports only an allowlisted phase and coarse rejection class on FAIL; exception messages, types, stacks, paths, Handles, semantic IDs and drawing content remain excluded. Rerun once on a fresh disposable copy with an empty outside-repository artifact directory and the final clean exact `main` SHA/DLL; report only the controlled `failure_phase`/`failure_code` if it still fails. P02 and overall LOCAL-002 remain `PENDING_LOCAL` until the complete PASS contract succeeds.

The V2 rerun at clean exact SHA `7c160de66de68c811282f4cd460e927370e454cd` returned only `failure_phase=DOOR_NATIVE_GEOMETRY` and `failure_code=STATE_REJECTED`; the source-copy hash and all process/script/sidecar/backup/metadata cleanup invariants passed. The phase boundary and source audit isolated a production placement defect: V25 `Solid3d.CreateBox` is centered, while the panel helper applied another negative half-extent displacement before rotating and translating to the already computed panel center. Source fix `#850` removed only that duplicate displacement and statically requires create -> rotate -> target-center placement without changing the plan, matching tolerance or opening intersection checks. That diagnostic and source correction were not P02 evidence by themselves; the subsequent licensed PASS below supplies the bounded result.

The fresh licensed rerun passed at clean exact SHA `7b4a379da15c8c0bed60536bc0ccca7334eb4712` with BricsCAD `25.2.10` and x64 Release adapter SHA-256 `25B6A40F120028CED160F5F04362FFAE1FBEA25E0A850CEE45860E761559B53F`. The Door case reconstructed 15 source cells and 16 positive output fragments, with one fully removed and five partially clipped source cells; all 16 native extents uniquely matched the authoritative plan and native/opening positive-area intersections were zero. The WallOpening case removed all 15 source cells and retained the explicit healthy `Complete`, opening-aware zero-output/zero-handle state. Ownership sets were disjoint, panel Health reported zero issues, and Locate resolved one generated panel to one canonical GlassWall owner. The repository-generated disposable DWG SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; the launched process exited, the private script was deleted, and no sidecar or backup remained. This promotes only P02 to bounded `LOCAL_PASS`; P03-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

### P03 guarded straight-path handoff

`QS3DCURTAINPATHPREPARE`, `QS3DCURTAINPATHPROBE` and `scripts/test-bricscad-v25-curtain-panel-path.ps1` prepare one bounded `LOCAL-002 / P03` run on a synthetic legacy/no-Level open straight-segment POLYLINE GlassWall. The runner seeds a 4 m positive-X segment followed by a 3 m positive-Y segment, reselects only the canonical source and runs production `QS3DCURTAIN3D`. The probe requires three source vertices, two non-bulged +Z WCS-XY segments and 7 m total length; canonical `PathPanelSolids` / `OpenPolyline` / `Complete` metadata; positive authoritative pieces on both path segments; and equal Handle, metadata, native and reconstructed path-piece counts. It independently reads every native panel `Solid3d.GeometricExtents` and uniquely matches its XY/Z bounds to the authoritative `CurtainWallDetailPlanner` plus `CurtainPathFramePlanner` result, including the rotated second segment. It also requires disjoint source/host/frame/panel ownership, zero blocking Core/live/runtime panel Health and one generated panel locating to one canonical GlassWall owner.

Run `scripts/preflight-curtain-panel-path-runtime-probe.py` first. The runner requires the exact repository x64 Release V25 DLL from a clean exact SHA, a nonblank initialized profile, a fresh guarded disposable copy and an empty artifact directory outside the repository. It starts only one hidden BricsCAD PID, deletes its private script, restores environment variables and verifies process, sidecar, backup and unchanged-DWG cleanup before accepting PASS or reporting an allowlisted `failure_phase` / `failure_code`. Markers and metadata exclude raw Handles, semantic IDs, profiles, local paths, exception details and drawing content. The guarded boundary remains required for requalification. This cell does not qualify P04 bulge/sagitta behavior, Level placement, openings, rebuild/stale, failure injection, Undo or save/reopen.

The first licensed `QS3D_CURTAIN_PANEL_PATH_RUNTIME_V1` P03 run passed at clean exact SHA `83b3f93274a60e8de3744cb8ae668ca7de381e5b` with BricsCAD `25.2.10` and x64 Release adapter SHA-256 `F42262DB54C21CEB4950F7CB9389D6BCB4830C4055EC2DB5013F5FB16AB62F6B`. The three-vertex/two-segment 7 m source produced 18 source panel cells and 21 authoritative path pieces: 12 on segment 0 and 9 on segment 1. All 21 native panel extents uniquely matched the path plan; native, Handle and metadata counts agreed. The build recorded canonical `PathPanelSolids` / `OpenPolyline` / `Complete` state with one host, 15 frames and 21 panels. Ownership sets were disjoint, panel Health reported zero issues and Locate resolved one panel to one canonical GlassWall owner. Source geometry and the disposable DWG SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` were preserved; launched-process exit, private-script deletion and sidecar/backup absence passed. This promotes only P03 to bounded `LOCAL_PASS`; P04-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

### P04 guarded bulged-path handoff

`QS3DCURTAINBULGEDSEED`, `QS3DCURTAINBULGEDPREPARE`, `QS3DCURTAINBULGEDPROBE` and `scripts/test-bricscad-v25-curtain-panel-bulged-path.ps1` prepare one bounded `LOCAL-002 / P04` run on a synthetic legacy/no-Level open bulged WCS-XY POLYLINE. The automation seed creates `(0,0) -> (4 m,0) -> (7 m,0)` with exact bulge `1` on the first chord, selects it for production `QS3DGLASSWALL`, configures effective `WallArcSagittaM=0.001`, reselects the canonical source and leaves production `QS3DCURTAIN3D` as the sole host/frame/panel builder. The probe independently requires the radius-2 m semicircle to produce exactly 50 bounded curved chords under the configured 1 mm sagitta and documented 10-degree ceiling, followed by one straight segment. It verifies every curved point lies on the source radius, every chord stays within the sagitta, path/source/piece budgets remain bounded, and authoritative panel pieces cover more than one curved segment plus the straight segment.

The probe reconstructs the production tessellated centerline and `CurtainWallDetailPlanner` plus `CurtainPathFramePlanner` plan, then uniquely matches every owned native panel `Solid3d.GeometricExtents` to one expected rotated prism AABB. Native, Handle, metadata and plan counts must agree with canonical `PathPanelSolids` / `OpenPolyline` / `Complete` / zero-opening state. It also requires the exact persisted sagitta, disjoint source/host/frame/panel ownership, zero blocking Core/live/runtime panel Health and Locate of one generated panel to one canonical GlassWall. This is intentionally evidence for bounded tessellated straight prisms, not exact swept-curve glass; it does not qualify corner aesthetics, P05-P12, Level placement or broad LOCAL-002 parity.

Run `scripts/preflight-curtain-panel-bulged-path-runtime-probe.py` first. The runner accepts only the exact repository x64 Release V25 DLL from a clean exact SHA, a nonblank initialized profile, a fresh guarded disposable copy and an empty artifact directory outside the repository. It launches only one hidden BricsCAD PID, deletes its private script, restores environment variables and verifies process, sidecar, backup and unchanged-DWG cleanup before accepting PASS or reporting an allowlisted phase/coarse rejection code. Markers and metadata are aggregate-only and explicitly publish `tessellated_fragments_only=true` plus `exact_swept_curve_qualified=false`; they exclude raw Handles, semantic IDs, profiles, local paths, exception details and drawing content. `QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1` remains the guarded requalification contract.

The first licensed P04 run passed at clean exact SHA `40502704b402b1aa55300f7f187b4fabd355eb40` on BricsCAD `25.2.10` with x64 Release adapter SHA-256 `2DB775C7708B57DD48CF1DEE3C454C7AD5DDBCE22B54C97EBDDE5A53DFDDF530`. The three-vertex/two-raw-segment source used a radius-2 m bulged chord at configured sagitta 1 mm; it produced exactly 50 curved chords plus one straight path segment. Twenty-four source cells mapped to 168 authoritative/native panel pieces: 159 curved pieces spanning all 50 curved segments and 9 straight pieces. All 168 native extents uniquely matched the plan, budgets and chord sagitta stayed within limits, metadata/native counts agreed, Health reported zero issues and Locate resolved one panel to one canonical owner. One host, 215 frames and 168 panels remained ownership-disjoint. Source geometry and disposable DWG SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` were preserved; process/script/sidecar/backup cleanup passed. This promotes only bounded tessellated P04 to `LOCAL_PASS`; it explicitly does not qualify an exact swept curve. P05-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

### P05 guarded stale/rebuild handoff

`QS3DCURTAINSTALE*` and `scripts/test-bricscad-v25-curtain-panel-stale-rebuild.ps1` prepare one ordered bounded `LOCAL-002 / P05` run on two synthetic legacy/no-Level LINE GlassWalls plus one initially unlinked Door. Production `QS3DGLASSWALL` / `QS3DDOOR` capture the sources, production `QS3DCURTAIN3D` performs every native build, production `HostLinkService` owns the opening relationship and production `QS3DSYNCSOURCE` reconciles the direct CAD source edit. The automation-only commands mutate and measure state; they do not duplicate production builders or Health/fingerprint policy.

The target owner is rebuilt after grid, panel depth, height and linked-opening changes. Before each rebuild the probe requires the exact old panel-set stale snapshot, Core `CURTAIN_PANEL_GENERATED_STALE`, the appropriate live config/geometry stale signal and clean native ownership. For these legacy/no-Level owners, grid/depth/height are semantic configuration changes: all three must raise `CURTAIN_PANEL_CONFIG_STALE` while preserving the CAD-only live geometry fingerprint; linking the Door changes both config and live geometry fingerprints. After each replacement the probe requires stale cleared, clean Health, disjoint old/new target panel sets with no old panel left live, refreshed configuration state, canonical count/grid/depth/height/area metadata and unique native AABB matches to the authoritative unclipped or opening-clipped plan. Grid changes must alter grid/count/area; depth-only changes must preserve count/clear area while changing native depth; height changes must alter rows/count/area; the Door relationship must reduce clear area without positive-area glass/opening overlap. The unrelated control GlassWall must retain its exact source/host/frame/panel sets, config/live fingerprints, metadata and native bounds throughout.

The final source edit deliberately distinguishes a raw CAD drift sample from semantic owner stale state. Extending the target LINE from 5 m to 6 m must leave the owner stale flag/snapshot absent and semantic length at its previous 5 m sample while live Health reports `CURTAIN_PANEL_LIVE_GEOMETRY_STALE` plus config drift. `QS3DSYNCSOURCE` must then reconcile semantic length to 6 m and ownership-safely remove the invalidated target host/frame/panel output before target-only `QS3DCURTAIN3D` creates the final clean set. This distinction prevents a live-source fingerprint warning from being misreported as evidence that semantic mutation APIs ran.

Run `scripts/preflight-curtain-panel-stale-rebuild-runtime-probe.py` first. The runner accepts only the exact clean-SHA repository x64 Release V25 DLL, a nonblank initialized profile, the guarded `*.curtain-stale-rebuild-probe-copy.dwg` suffix and an empty artifact directory outside the repository. It launches one hidden PID, deletes its private script, restores environment variables and verifies process/sidecar/backup/unchanged-DWG cleanup before PASS or sanitized allowlisted FAIL. Markers contain aggregate transition/count/area evidence only, never raw Handles, semantic IDs, profiles, paths, exceptions or drawing content. `QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1` remains a handoff contract: P05 and overall LOCAL-002 stay `PENDING_LOCAL` until a fresh licensed exact-SHA run returns its complete PASS/cleanup result. It does not qualify arbitrary edit ordering, P06-P12, Level placement, Undo or save/reopen.

### P01 sanitized local evidence

At exact clean SHA `3da7b20013233a71eb174c77e87d4618b370ebd4`, the exact x64 Release adapter (`C2A0E60131B6A2E348728937C0EF47E549AB26D028F3B69CE48CBA686B6FE2A6`) ran on BricsCAD `25.2.10` against an ordinary disposable copy of the repository-generated sample. `QS3DDRAWGLASSWALL` plus `QS3DCURTAIN3D` produced one backing host, ten frame solids and fifteen panel solids. The panel metadata count was also fifteen; source/host/frame/panel ownership sets were disjoint; Core, live-fingerprint and native-marker panel health had zero blocking issues; and selecting one generated panel resolved to exactly one canonical GlassWall owner. The disposable DWG hash remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` before and after, no QSDB sidecar remained, and the launched BricsCAD process was cleaned up. This result covers only the P01 positive LINE/no-opening cell. It does not cover panel bounds by independent measurement, opening clipping, path/bulge geometry, failure injection, Undo, save/reopen or multi-DWG behavior; P02-P12 and overall LOCAL-002 therefore remain pending.

The hardened runner requalified the same bounded P01 case at exact clean SHA `53a4490f245774e9253d24ba70799b4311ff7e12` with x64 Release adapter SHA-256 `644E24154161ADF0DE31A49E17FBB0FF65BB1B9A6251EC11594EA1E2E4924EAF`. BricsCAD `25.2.10` again produced one host, ten frame solids and fifteen panel solids with matching metadata, disjoint source/host/frame/panel ownership, zero panel-health issues and one canonical Locate owner. The generated sample DWG SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; no sidecar was created, the private runtime `.scr` was deleted and the launched-process exit was verified before PASS publication. This supersedes the earlier P01 evidence for current-candidate reproducibility only; it does not promote P02-P12.

## Non-goals

This contract does not claim:

- arbitrary 3D/tilted/closed/freeform Curtain paths;
- exact curved swept glass rather than bounded tessellated fragments;
- fabrication, manufacturer or structural design rules;
- automatic completion of unsupported opening booleans;
- V25 runtime qualification from source/static evidence.
