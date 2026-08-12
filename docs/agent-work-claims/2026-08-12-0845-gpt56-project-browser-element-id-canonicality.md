# Work claim — Project Browser element ID canonicality

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T08:45:30+07:00`
- Baseline main SHA: `8527832439473bf636e223a938273e77cbd351e1`
- Priority: owner-requested continue-all source-safe bug fixing

## Reserved scope

Fail closed when `ProjectBrowserPlanner.Build()` receives a semantic `ProjectElement.Id` with surrounding whitespace. The current planner uses `Trim()` for duplicate detection but can still emit the raw padded ID into the browser tree, which downstream selection/workspace canonical-ID validation then rejects.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserPlanner.cs`
- focused deterministic Project Browser/Core regression or preflight surface covering padded semantic element IDs

## Excluded scope

- Project Browser workspace XML/null-metadata/query/reference lanes already completed or owned by other agents
- Project Browser UI/runtime changes
- BricsCAD licensed runtime, NETLOAD/DemandLoad, private DWG, packaging, signing, performance and GitHub Actions
- tie/slab/generated-solid health, semantic view kind, licensing UTC token, Floor Zone, baseline severity and other currently claimed lanes

## Validation plan

- prove a canonical element ID still builds normally
- prove leading/trailing-whitespace semantic element IDs fail closed at `ProjectBrowserPlanner.Build()` before a malformed tree is produced
- use deterministic Core/static regression only; do not dispatch GitHub Actions or claim BricsCAD runtime PASS

## Coordination

Reviewed current recent claim/commit activity and Project Browser history at the baseline. Recent browser workspace null-metadata and reference-canonicality lanes are completed; this reservation is limited to raw semantic element ID canonicality at the base Project Browser tree boundary and excludes neighboring active claims.

## Completion condition

Source fix plus focused regression is pushed to `main`, the behavior is read back from current `main`, and this claim is marked `COMPLETED`; otherwise mark it `RELEASED` without claiming completion.
