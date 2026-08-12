# Work claim — Semantic untrack predicate structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-untrack-predicate-structural-freshness`
- Registered: `2026-08-12T11:05:00+07:00`
- Baseline main SHA: `8054e291cabf8f49b2d2afdc3bb61df9155a6969`
- Priority: P1 — fail closed when caller predicates structurally mutate project element ownership without advancing ChangeVersion.

## Confirmed defect

`SemanticUntrackService.Untrack()` already rejects `ProjectState.ChangeVersion` changes while evaluating the optional predicate, but `ProjectState.Elements` is a publicly mutable `IList<ProjectElement>`. A predicate can remove or replace elements directly without calling `Touch()`, so `ChangeVersion` remains unchanged. A false predicate can then escape through the zero-target no-op path after changing project ownership, while a true predicate can continue into dependency planning with a detached/stale target.

## Reserved scope

- `src/QS3D.Core/Services/SemanticUntrackService.cs`, limited to project element ownership freshness around predicate evaluation
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Snapshot project element ID -> instance ownership immediately before predicate evaluation.
- After predicate materialization and existing ChangeVersion validation, reject any element collection count, identity, null/duplicate, removal, or replacement drift before the zero-target path or dependency planning.
- Preserve stable predicate behavior, existing revision freshness semantics, dependency blocking, rollback behavior, caller predicate side effects, and successful untrack mutation semantics.
- Regression must cover structural mutation with both true and false predicate results, including unchanged `ChangeVersion` proving the ownership guard—not the existing revision guard—causes rejection.

## Excluded scope

- General dependency graph changes.
- Semantic handle ownership resolver changes.
- ProjectState collection encapsulation.
- UI, BricsCAD runtime, persistence, exporter, or GitHub Actions changes.
