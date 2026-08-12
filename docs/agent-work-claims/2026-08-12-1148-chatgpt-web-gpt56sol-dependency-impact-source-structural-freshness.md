# Work claim — Dependency impact source structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-source-structural-freshness`
- Registered: `2026-08-12T11:48:00+07:00`
- Baseline main SHA: `a6e89584974c7ece6a62502cb89e9f2dcf78c22d`
- Priority: P1 — reject lazy source-ID enumeration that structurally changes project element ownership without advancing ChangeVersion.

## Confirmed defect

`DependencyImpactPlanner.Plan()` now captures `ProjectState.ChangeVersion` before enumerating `sourceElementIds`, but `ProjectState.Elements` remains a publicly mutable `IList<ProjectElement>`. A caller-controlled lazy source-ID enumerable can remove or replace project elements directly without calling `Touch()`, leaving `ChangeVersion` unchanged. The planner then builds its `DependencyGraph` from the already-mutated project and can return an apparently fresh impact plan that silently omits the pre-enumeration dependent structure.

Concrete counterexample: a project contains `ROOT` and `CHILD`, where `CHILD.DependsOn = [ROOT]`. During source-ID enumeration, the enumerable removes `CHILD` directly and then yields `ROOT`. `ChangeVersion` stays unchanged, graph rebuild sees only `ROOT`, and the current planner can return a zero-impact plan instead of rejecting the stale structural input.

## Reserved scope

- `src/QS3D.Core/Services/DependencyImpactPlanner.cs`, limited to element-ownership freshness around caller source-ID enumeration/planning
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Snapshot project element ID -> instance ownership before enumerating caller-supplied source IDs.
- After materializing/canonicalizing the source IDs, reject project element count, null/duplicate identity, removal, or same-ID replacement drift even when `ChangeVersion` is unchanged, before graph planning.
- Re-check structural ownership before returning the plan so a plan cannot be stamped fresh against a structurally different project.
- Preserve the existing ChangeVersion freshness error/semantics, source-ID validation, caller side effects, graph validation, impact depth/cause/root ordering, and stable valid plans.
- Regression must prove direct removal and same-ID replacement are rejected with unchanged `ChangeVersion`, while a stable ROOT -> CHILD plan still succeeds.

## Excluded scope

- `DependencyGraph` validation/cycle logic.
- Reporting, quantity selection, ProjectState collection encapsulation, UI, BricsCAD runtime, persistence, exporter, or GitHub Actions changes.
