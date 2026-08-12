# Work claim — DependencyGraph rebuild input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-graph-rebuild-input-freshness`
- Registered: `2026-08-12T10:41:00+07:00`
- Baseline main SHA: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Priority: P1 — fail-closed stateful graph rebuild at a caller-controlled reentrant enumeration boundary.

## Confirmed defect

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` materializes a new dependency graph from caller-controlled lazy input and then replaces the current `_dependents` / `_elementsById` state. During enumeration, the producer can reentrantly call `Rebuild()` on the same `DependencyGraph`. The inner rebuild can complete and install a newer graph, after which the outer rebuild resumes and overwrites that newer state using stale materialized input.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to rebuild freshness/revision handling
- focused Core smoke regression and ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-dependency-graph-rebuild-input-freshness.md`
- this claim file

## Intended contract

- Track successful graph rebuilds with a private monotonic revision.
- Capture the revision immediately before enumerating caller-supplied rebuild elements.
- After enumeration and existing validation, reject revision drift before clearing or replacing either graph dictionary.
- Prepare the checked next revision before the first graph mutation, then apply it only after both graph dictionaries are replaced.
- Every successful rebuild advances the revision so any successful inner rebuild invalidates an outer reentrant build.
- Preserve current null/duplicate/dependency validation, missing-dependency rejection, case-insensitive identity, direct/transitive lookup, and topological ordering behavior.

## Excluded scope

- Cross-thread synchronization/thread-safety guarantees.
- `ProjectState.ChangeVersion` semantics.
- Dependency validation/topological algorithm changes unrelated to rebuild freshness.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.
