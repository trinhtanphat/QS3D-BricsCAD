# Work claim — MAP-02 project-owned mapping coverage integration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map02-project-owned-coverage-20260813`
- Registered UTC: `2026-08-13T17:20:00Z`
- Baseline main SHA: `a9ab39b416d8dbab44ed7319db405d250a56f10a`
- Priority: `MAP-02 P0/P1`

## Verified gap

MAP-01B now persists canonical measurement/work-item mappings as project-owned state, but `MeasurementWorkItemCoverageEvaluator` still only exposes `Evaluate(ProjectState, MeasurementWorkItemMappingCatalog)`. A caller can therefore evaluate a project against a detached/stale/different catalog instead of the project's canonical mapping state.

## Reserved scope

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- one focused self-registering Core smoke regression
- this claim file

## Bounded implementation

- add a project-owned `Evaluate(ProjectState)` entry point that constructs the canonical catalog from `project.MeasurementWorkItemMappings` and delegates to the existing evaluator;
- preserve the existing explicit-catalog overload for intentional preview/scenario evaluation;
- do not change coverage issue semantics, mapping contract semantics, reporting/UI, persistence, rates/cost, geometry, or native surfaces;
- regression must prove project-owned mappings are consumed and an intentionally different external catalog cannot influence the new overload.

## Validation policy

No GitHub Actions will be dispatched. Managed/native PASS will only be reported if actually executed. No force-push.
