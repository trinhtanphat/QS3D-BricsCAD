# Work claim — Dependency Impact root enumeration bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-root-bound`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `b0bec89cccb5d0cece58d187ea6c28aa60e761ae`
- Priority: P1 — fail closed on impossible/unbounded caller root sequences in a public read-only Core planner.

## Confirmed defect

`DependencyImpactPlanner.Plan(ProjectState, IEnumerable<string>)` materializes caller-provided root IDs through `CanonicalRoots(...)` with an unbounded `foreach`. A lazy/infinite sequence of distinct strings can therefore run forever or grow memory without limit even though a valid request can never contain more distinct roots than the project has semantic elements.

## Reserved scope

- `src/QS3D.Core/Services/DependencyImpactPlanner.cs`
- `tests/QS3D.Core.SmokeTests/DependencyImpactPlannerSmoke.cs`
- `scripts/preflight-dependency-impact-plan.py`
- this claim file for close-out

## Intended contract

- Root enumeration stops and fails closed as soon as the request exceeds the project element count.
- The bound is derived from the canonical project cardinality rather than an arbitrary new product limit.
- Existing canonical ID, duplicate, missing-root, deterministic BFS, read-only and `ChangeVersion` contracts remain unchanged.
- A focused smoke proves an over-bound lazy source is not enumerated past the first impossible item.

## Excluded scope

No DependencyGraph rewrite, no regeneration/apply mutation, no BricsCAD/native/UI changes, no GitHub Actions dispatch, and no licensed V25 runtime claim.

## Completion condition

The public planner rejects over-bound root enumeration without over-consuming the caller source, focused regression/static coverage is on `main`, and the claim is closed with exact commit SHAs and truthful validation notes.
