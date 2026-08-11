# Agent work claim — Plan-to-3D commit-time source freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-plan-to-3d-source-freshness`
- Registered: `2026-08-11T21:00:00+07:00`
- Baseline main SHA observed before reservation: `650300db165f14f70fae688678cd4838bf57c5d7`
- Claim registration commit: `3013cecbe61edd97a8a7db0affc3857c4ee285b7`
- Rebased implementation commit: `6021af621ce56eb5728c63ecf2d61cbb05c52234`
- Pull request: `#485`
- Squash merge on `main`: `2e1cbe4af5eab21b8a120a11f8dd1103b7ca8c9e`
- Mode: remote source/static only; no licensed BricsCAD V25 runtime PASS is claimed.

## Confirmed defect

`PlanTo3DCommands.ConvertPlanWalls(...)` re-read LINE/open-POLYLINE geometry after prompting, but after `DirectDrawProjectPreviewContext.ResolveForMutation(...)` it proceeded to semantic freshness and `ProjectStateSnapshot.Capture(project)` without a second CAD geometry fingerprint re-read. A source edit landing in the preview-context resolution window could therefore cross the final commit boundary without being rejected.

## Reserved scope

- `src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs`
- `scripts/preflight-plan-to-3d-source-geometry-freshness.py`
- this claim file for close-out

## Implemented contract

- preserved the existing initial and post-prompt source preflights;
- after `ResolveForMutation(...)`, `ConvertPlanWalls(...)` now re-runs `PreflightSources(...)`, compares the deterministic geometry fingerprints, and replaces the working snapshot only after the comparison succeeds;
- semantic/generated-source freshness and `ProjectStateSnapshot.Capture(project)` remain after that final CAD geometry guard;
- preserved project identity/ChangeVersion/unit/UCS guards, closed-POLYLINE fail-closed behavior, scoped regeneration, generated ownership and rollback semantics;
- strengthened `preflight-plan-to-3d-source-geometry-freshness.py` to require three source preflights and the exact post-resolve re-read/compare/assign/semantic-freshness/snapshot ordering.

## Validation and boundary

- connector-reviewed squash diff contains only the intended three-line PlanTo3D commit-boundary guard plus the focused static-preflight hardening;
- PR `#485` merged successfully with expected head SHA and squash merge `2e1cbe4af5eab21b8a120a11f8dd1103b7ca8c9e`;
- the merge remains an ancestor of later observed `main` and later commits did not overwrite the two implementation paths at close-out review;
- the container could not resolve GitHub raw-content DNS, so no local execution of the Python gate is claimed;
- no GitHub Actions workflow was dispatched or re-run;
- BricsCAD V25 build/NETLOAD/interactive qualification remains `LOCAL_ONLY`; `LOCAL-008` and `LOCAL-014` remain pending local evidence.

## Completion

Completed and reservation released. The post-resolve CAD source-fingerprint guard and focused regression contract are integrated on `main`; future agents may edit these files only after re-checking current active/BLOCKED claims.
