# Work claim — Dependency Impact input-enumeration freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-input-freshness`
- Registered: `2026-08-11T23:58:00+07:00`
- Baseline main SHA: `f3dc5be32f3bd86d1e8e617c788f50a59af24896`
- Priority: P1 — make the existing read-only `ChangeVersion` freshness contract cover caller root enumeration as well as graph traversal.

## Confirmed defect

`DependencyImpactPlanner.Plan(...)` currently captures `project.ChangeVersion` only after `CanonicalRoots(...)` has enumerated the caller-provided `IEnumerable<string>`. If project state changes while that potentially lazy input is being enumerated, the planner records the post-change version and its final freshness check cannot detect that the project changed during the operation. The new root cardinality bound also reads project element count before that window, so root validation and graph planning can observe different project revisions without failing closed.

## Reserved scope

- `src/QS3D.Core/Services/DependencyImpactPlanner.cs`
- `tests/QS3D.Core.SmokeTests/DependencyImpactPlannerSmoke.cs`
- `scripts/preflight-dependency-impact-plan.py`
- this claim file for close-out

## Intended contract

- Snapshot `ChangeVersion` and semantic element cardinality before caller root enumeration.
- Use that captured cardinality for the root bound.
- Existing final `ChangeVersion` check must reject any project mutation that occurs during root enumeration or later graph planning.
- Preserve canonical root validation, deterministic traversal, read-only normal-path behavior and the newly-added bounded-enumeration contract.
- Add deterministic regression using a lazy root source that mutates/touches the project while enumerating and prove the planner rejects the stale operation.

## Excluded scope

No DependencyGraph rewrite, no mutation workflow, no BricsCAD/native/UI changes, no Actions dispatch, and no V25 runtime claim.

## Completion condition

The planner's freshness window begins before any caller-controlled enumeration, focused regression/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation boundaries.
