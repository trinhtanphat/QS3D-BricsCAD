# Work claim — DependencyGraph dirty-order input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-dirty-order-input-freshness`
- Registered: `2026-08-12T14:09:00+07:00`
- Baseline main SHA: `6450b35f25ecc268c2ed09ec9e3707f4092bc1b7`
- Priority: P1 — make dirty ordering observe one stable post-enumeration dirty-state snapshot instead of caller-controlled intermediate states.

## Confirmed defect

`DependencyGraph.TopologicalDirtyOrder(IEnumerable<ProjectElement>)` previously validated each element and read `element.Dirty` inside the caller-controlled input enumeration. An iterator could therefore mutate the dirty state of an element that was already yielded before yielding a later element. The returned dirty subset then reflected an intermediate state rather than the state after the input sequence had been completely materialized.

Concrete counterexample: yield element A while A is clean, then call public `A.MarkDirty(...)`, then yield element B. The previous implementation permanently excluded A because it sampled `A.Dirty` before the iterator mutation, even though A was dirty when enumeration completed.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to `TopologicalDirtyOrder()` input materialization / dirty-state snapshot timing
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Fully materialize the input sequence before sampling any element dirty flags.
- After materialization, build the dirty subset from the then-current state and preserve existing topological ordering semantics.
- Keep null-element, dependency validation, duplicate dirty-element ID, cycle detection, and subset-only dependency traversal behavior unchanged.
- Regression must prove a caller iterator mutation performed during enumeration is observed after enumeration completes, and that stable clean/dirty inputs preserve existing order semantics.

## Excluded scope

- `DependencyGraph.Rebuild()` freshness semantics.
- Dependency mutation/cycle validation beyond the existing contract.
- Persistence, regeneration policy, UI, BricsCAD runtime, exporter, or GitHub Actions changes.

## Validation

- Implementation + focused regression commit: `81653ee5b12f793685c9690babcd4e2bd8a20ebf` (`fix(core): snapshot dirty order after input enumeration`).
- Read back `src/QS3D.Core/Services/DependencyGraph.cs` from current `main`: caller input is fully materialized before the dirty subset reads `element.Dirty`; existing topological ordering, duplicate-ID, cycle, and dependency traversal code remains in place.
- Read back `tests/QS3D.Core.SmokeTests/DependencyGraphDirtyOrderInputFreshnessSmoke.cs` from current `main`: the focused smoke auto-registers and covers iterator-time clean→dirty mutation plus stable dependency-first ordering and clean-element exclusion.
- The implementation commit is an ancestor of the current `main` lineage observed during closeout.
- No GitHub Actions were dispatched. No executable local/build or licensed BricsCAD runtime PASS is claimed from this connector-only closeout.
- No remaining LOCAL_ONLY or policy gate was introduced by this lane.

## Completion condition

Satisfied by pushed implementation/regression `81653ee5b12f793685c9690babcd4e2bd8a20ebf` and this completion record on `main`.
