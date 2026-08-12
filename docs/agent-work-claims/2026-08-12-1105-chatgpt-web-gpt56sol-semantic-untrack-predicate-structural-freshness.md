# Work claim — Semantic untrack predicate structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-untrack-predicate-structural-freshness`
- Registered: `2026-08-12T11:05:00+07:00`
- Completed: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `8054e291cabf8f49b2d2afdc3bb61df9155a6969`
- Claim merge SHA: `94626d24848b59b37b4d388f291d5b1c4fbb6efd`
- Implementation SHA: `de2ceea54c67e0a23d5abc096fc3a8bd304282bd`
- PR: `#803`
- Priority: P1 — fail closed when caller predicates structurally mutate project element ownership without advancing ChangeVersion.

## Confirmed defect

`SemanticUntrackService.Untrack()` already rejected `ProjectState.ChangeVersion` changes while evaluating the optional predicate, but `ProjectState.Elements` is a publicly mutable `IList<ProjectElement>`. A predicate could remove or replace elements directly without calling `Touch()`, leaving `ChangeVersion` unchanged. A false predicate could then escape through the zero-target no-op path after changing project ownership, while a true predicate could continue into dependency planning with a detached/stale target.

## Implemented contract

- Snapshot project element ID -> instance ownership immediately before predicate evaluation.
- After predicate materialization and existing ChangeVersion validation, reject element collection count, identity, null/duplicate, removal, or replacement drift before the zero-target path or dependency planning.
- Stable predicate behavior, existing revision freshness semantics, dependency blocking, rollback behavior, caller predicate side effects, and successful untrack mutation semantics remain unchanged.
- `SemanticUntrackPredicateStructuralFreshnessSmoke` covers false-predicate target removal, true-predicate target replacement, and unrelated-element removal while confirming `ChangeVersion` remains unchanged so the structural guard is the rejecting boundary.

## Validation

- Claim merged to `main` as `94626d24848b59b37b4d388f291d5b1c4fbb6efd`.
- Implementation squash-merged PR `#803` to `main` as `de2ceea54c67e0a23d5abc096fc3a8bd304282bd`.
- Commit readback confirms the source ownership snapshot/validation and focused smoke file are present in the merged commit.
- Ancestry readback confirms the implementation remains an ancestor of subsequent `main` commits.
- GitHub combined status returned no status checks for the implementation commit (`statuses=[]`). No GitHub Actions were dispatched by this lane, and no BricsCAD runtime/build PASS is claimed.

## Reserved scope

- `src/QS3D.Core/Services/SemanticUntrackService.cs`, limited to project element ownership freshness around predicate evaluation
- `tests/QS3D.Core.SmokeTests/SemanticUntrackPredicateStructuralFreshnessSmoke.cs`
- this claim file

## Excluded scope

- General dependency graph changes.
- Semantic handle ownership resolver changes.
- ProjectState collection encapsulation.
- UI, BricsCAD runtime, persistence, exporter, or GitHub Actions changes.
