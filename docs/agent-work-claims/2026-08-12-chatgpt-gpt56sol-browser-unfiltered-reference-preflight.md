# Work claim — browser unfiltered reference preflight

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-browser-unfiltered-preflight`
- Registered: `2026-08-12T09:42:00+07:00`
- Baseline main SHA: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Priority: `Correctness regression discovered during requested continue-all review; ProjectBrowserQueryPlanner unfiltered mode bypasses Family/reference integrity validation that filtered mode still enforces.`

## Confirmed regression

The original Query Planner validated Family references before deciding whether a query was filtered. Commit `3644d795cd88552d1d4b53a20612159a51649183` optimized away an unnecessary unfiltered tree build but moved the full index/reference preflight after the unfiltered short-circuit. Current behavior can therefore accept a project with dangling or category-mismatched Family references when no filter is active while rejecting the same project when any filter/search is active.

## Reserved scope

Restore mode-independent query-planner integrity preflight without reintroducing duplicate tree construction. Preserve current filtered behavior, bounds, canonical reference validation, query semantics, grouping, and the performance optimization of building only the required tree.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryPlannerSmoke.cs`

## Validation plan

- Keep query/category normalization before determining filtered state.
- Build/validate Family/Floor/Zone indexes and element references before the unfiltered return.
- Preserve exactly one `ProjectBrowserPlanner.Build` call on each execution path.
- Add focused regressions proving unfiltered mode rejects a missing Family and a Family/category mismatch while a healthy unfiltered query still returns the whole tree.
- No GitHub Actions; no BricsCAD runtime/build PASS claims.

## Completion condition

Claim lands on main before implementation; source and regression commits use current blob SHAs; claim closes `COMPLETED` with exact evidence and no dangling ownership.
