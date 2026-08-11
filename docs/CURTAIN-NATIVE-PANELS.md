# Curtain native panels — source contract and local V25 matrix

Updated: 2026-08-11 (UTC+7)

## Status boundary

This document defines the clean-room QS3D contract for panel-by-panel native glass inside the BricsCAD-hosted Curtain workflow.

The repository source/static gate may prove that the ownership, planning, rollback, health and command wiring are present. It cannot prove BricsCAD `Solid3d` geometry, nested-transaction behavior, opening clearance, selection/Locate, Undo, or save/reopen behavior. Each row remains `LOCAL-002 / PENDING_LOCAL` until a licensed BricsCAD V25 run records sanitized evidence against one clean exact SHA. P01 now has such a bounded runtime result; it does not promote the remaining matrix or overall LOCAL-002 status.

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

| Case | Minimum scenario | Required result/evidence | Status before local run |
|---|---|---|---|
| P01 | LINE GlassWall, multiple grid cells, no opening | backing host + frame + panel layers are distinct; panel count/geometry/ownership agree | LOCAL_PASS at `3da7b20013233a71eb174c77e87d4618b370ebd4` (bounded basic case only) |
| P02 | LINE with Door/WallOpening fully and partially intersecting cells | no panel crosses the opening; positive fragments only; full-cover either records an explicit healthy complete-empty state or refuses before replacement according to the final source contract | PENDING_LOCAL |
| P03 | open straight-segment POLYLINE | station-mapped panel fragments follow every path segment; owner resolves to one GlassWall | PENDING_LOCAL |
| P04 | open bulged WCS-XY POLYLINE | bounded tessellated fragments follow the configured sagitta contract; no unbounded growth | PENDING_LOCAL |
| P05 | grid, depth, height and linked-opening change followed by rebuild | panel stale state appears before rebuild and clears only after valid replacement | PENDING_LOCAL |
| P06 | one missing old handle, duplicate canonical handle, foreign/unmarked solid and cross-owner conflict | every case refuses before erase/append; surviving old set and semantic metadata remain unchanged | PENDING_LOCAL |
| P07 | panel/fragment budget exceeded and malformed/off-host opening provenance, including failure on a later selected element | fail closed before mutation of the invalid element; the outer transaction rolls back the whole batch so every previous valid output remains intact | PENDING_LOCAL |
| P08 | injected failure at semantic regeneration and each of six host/frame/panel phases | pre-commit failure leaves native and semantic snapshots unchanged; no partial phase commit | PENDING_LOCAL |
| P09 | injected live-fingerprint/UI failure after outer commit | committed geometry remains; truthful warning directs Health/Release review | PENDING_LOCAL |
| P10 | select a generated panel, Locate/Family review, Health All and Release Check | canonical GlassWall owner is resolved and panel health participates in both aggregate checks | PENDING_LOCAL |
| P11 | Undo, save/reopen and rebuild | native counts/ownership/fingerprints remain coherent; no foreign deletion | PENDING_LOCAL |
| P12 | two open DWGs with modeless Curtain Hub | command and refresh remain bound to the intended active DWG/project | PENDING_LOCAL |

For every case record before/after aggregate native counts, semantic `ChangeVersion`/stale state, result code and relevant health-code set. Screenshots may remain local; if a sanitized summary is committed, it must be tied to the exact SHA and must not contain customer content.

### P01 sanitized local evidence

At exact clean SHA `3da7b20013233a71eb174c77e87d4618b370ebd4`, the exact x64 Release adapter (`C2A0E60131B6A2E348728937C0EF47E549AB26D028F3B69CE48CBA686B6FE2A6`) ran on BricsCAD `25.2.10` against an ordinary disposable copy of the repository-generated sample. `QS3DDRAWGLASSWALL` plus `QS3DCURTAIN3D` produced one backing host, ten frame solids and fifteen panel solids. The panel metadata count was also fifteen; source/host/frame/panel ownership sets were disjoint; Core, live-fingerprint and native-marker panel health had zero blocking issues; and selecting one generated panel resolved to exactly one canonical GlassWall owner. The disposable DWG hash remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` before and after, no QSDB sidecar remained, and the launched BricsCAD process was cleaned up. This result covers only the P01 positive LINE/no-opening cell. It does not cover panel bounds by independent measurement, opening clipping, path/bulge geometry, failure injection, Undo, save/reopen or multi-DWG behavior; P02-P12 and overall LOCAL-002 therefore remain pending.

## Non-goals

This contract does not claim:

- arbitrary 3D/tilted/closed/freeform Curtain paths;
- exact curved swept glass rather than bounded tessellated fragments;
- fabrication, manufacturer or structural design rules;
- automatic completion of unsupported opening booleans;
- V25 runtime qualification from source/static evidence.
