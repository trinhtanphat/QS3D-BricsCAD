# Work claim — Dependency impact source structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-source-structural-freshness`
- Registered: `2026-08-12T11:48:00+07:00`
- Completed: `2026-08-12T11:51:00+07:00`
- Baseline main SHA: `a6e89584974c7ece6a62502cb89e9f2dcf78c22d`
- Claim merge SHA: `6de6e0897aaefe068b0a968ef086ac1386eed085`
- Implementation SHA: `14b593976950ac5d40ed95ca6c4f4adcc56ea747`
- Implementation PR: `#842`
- Priority: P1 — reject lazy source-ID enumeration that structurally changes project element ownership without advancing ChangeVersion.

## Confirmed defect

`DependencyImpactPlanner.Plan()` captured `ProjectState.ChangeVersion` before enumerating `sourceElementIds`, but `ProjectState.Elements` remains a publicly mutable `IList<ProjectElement>`. A caller-controlled lazy source-ID enumerable could remove or replace project elements directly without calling `Touch()`, leaving `ChangeVersion` unchanged. The planner then built its `DependencyGraph` from the already-mutated project and could return an apparently fresh impact plan that silently omitted the pre-enumeration dependent structure.

Concrete counterexample: a project contains `ROOT` and `CHILD`, where `CHILD.DependsOn = [ROOT]`. During source-ID enumeration, the enumerable removes `CHILD` directly and then yields `ROOT`. `ChangeVersion` stays unchanged, graph rebuild would see only `ROOT`, and the old planner could return a zero-impact plan instead of rejecting the stale structural input.

## Implemented contract

- Snapshot project element ID -> instance ownership before enumerating caller-supplied source IDs.
- After materializing/canonicalizing source IDs, preserve the existing `ChangeVersion` rejection and additionally reject element count, null/duplicate identity, removal, or same-ID replacement drift before graph planning.
- Re-check version and structural ownership before returning the plan.
- Preserve caller side effects, source-ID validation, graph validation, deterministic impact depth/cause/root ordering, and stable valid plans.
- `DependencyImpactSourceStructuralFreshnessSmoke` covers direct removal, same-ID replacement with unchanged `ChangeVersion`, and a stable `ROOT -> CHILD` plan.

## Validation

- Claim-only PR `#839` squash-merged as `6de6e0897aaefe068b0a968ef086ac1386eed085` before source changes.
- Clean implementation PR `#842` changed exactly two files and squash-merged as `14b593976950ac5d40ed95ca6c4f4adcc56ea747`.
- Commit readback confirms the ownership guard and focused smoke are present in the merged commit.
- GitHub combined status returned no status checks (`statuses=[]`). No GitHub Actions or BricsCAD runtime/build PASS is claimed.
- A later duplicate claim `6eac72ba321660e2b632088b881d47e118fed208` appeared after this lane's claim-first merge. Its claim file was not modified by this lane; the later agent must reconcile against the now-merged implementation.

## Reserved scope

- `src/QS3D.Core/Services/DependencyImpactPlanner.cs`, limited to element-ownership freshness around caller source-ID enumeration/planning
- `tests/QS3D.Core.SmokeTests/DependencyImpactSourceStructuralFreshnessSmoke.cs`
- this claim file

## Excluded scope

- `DependencyGraph` validation/cycle logic.
- Reporting, quantity selection, ProjectState collection encapsulation, UI, BricsCAD runtime, persistence, exporter, or GitHub Actions changes.
