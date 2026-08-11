# Room Auto preview-to-commit freshness

Updated: 2026-08-11

## Defect

`QS3DROOMAUTO` performs a read-only preview phase before semantic mutation: it resolves Room boundary settings, acquires and metricizes CAD boundary segments, then runs topology diagnostics. The command already revalidated `ProjectId` and Room settings when a project existed at preview time.

The missing branch was the **no-project preview** path. If a canonical QS3D project became visible after preview/selection but before commit, the command could enter `GetOrCreate(...)` and apply boundaries computed with the no-project defaults to that newly appeared project. The same preview/commit gap could also allow the drawing-unit policy used to convert selected CAD geometry to differ from the unit policy active at mutation time.

## Source contract

The command now records the resolved `LengthUnit` used immediately after non-empty boundary selection.

After topology diagnostics prove at least one accepted face:

- if a project existed during preview, mutation still requires the canonical same-`ProjectId` project;
- if no project existed during preview, a newly visible existing project causes a fail-closed rerun request before `GetOrCreate(...)`;
- after either commit path resolves a project, the command revalidates the drawing-unit policy and all four Room boundary settings (`Tolerance`, `ArcSagitta`, `SplineChord`, `MinimumArea`) before `ProjectStateSnapshot` or semantic mutation;
- cancel, empty selection, insufficient/open topology, and below-minimum-area exits remain before project bind/create.

The no-project path remains intentionally creation-capable only when accepted geometry exists and no project became visible before commit.

## Static guard repair

`scripts/preflight-room-auto-project-lifecycle.py` had become stale after Room diagnostics were introduced: it still required the removed direct `RoomBoundaryEngine.Discover(...)` path and the old explicit empty-segment branch. This batch updates the gate to the current `RoomBoundaryDiagnosticService` flow and locks the new preview/commit freshness ordering.

## Local runtime evidence

Exact V25 behavior remains `LOCAL_ONLY`. The matching local qualification is merged into `LOCAL-001` in `docs/LOCAL-AGENT-INBOX.md`: exercise an initially projectless drawing, make a valid Room preview, then make a project visible before commit and verify refusal with no Room/audit/CAD mutation; separately change project Room settings or unit policy between preview and commit and verify the same fail-closed boundary.

No static/source result is a `LOCAL_PASS`.
