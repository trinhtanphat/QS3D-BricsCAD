# Work claim — DependencyGraph dirty-order input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-dirty-order-input-freshness`
- Registered: `2026-08-12T14:09:00+07:00`
- Baseline main SHA: `6450b35f25ecc268c2ed09ec9e3707f4092bc1b7`
- Priority: P1 — make dirty ordering observe one stable post-enumeration dirty-state snapshot instead of caller-controlled intermediate states.

## Confirmed defect

`DependencyGraph.TopologicalDirtyOrder(IEnumerable<ProjectElement>)` currently validates each element and reads `element.Dirty` inside the caller-controlled input enumeration. An iterator can therefore mutate the dirty state of an element that was already yielded before yielding a later element. The returned dirty subset then reflects an intermediate state rather than the state after the input sequence has been completely materialized.

Concrete counterexample: yield element A while A is clean, then call public `A.MarkDirty(...)`, then yield element B. The current implementation permanently excludes A because it sampled `A.Dirty` before the iterator mutation, even though A is dirty when enumeration completes.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to `TopologicalDirtyOrder()` input materialization / dirty-state snapshot timing
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Fully materialize and validate the input sequence before sampling any element dirty flags.
- After materialization, build the dirty subset from the then-current state and preserve existing topological ordering semantics.
- Keep null-element, dependency validation, duplicate dirty-element ID, cycle detection, and subset-only dependency traversal behavior unchanged.
- Regression must prove a caller iterator mutation performed during enumeration is observed after enumeration completes, and that stable clean/dirty inputs preserve existing order semantics.

## Excluded scope

- `DependencyGraph.Rebuild()` freshness semantics.
- Dependency mutation/cycle validation beyond the existing contract.
- Persistence, regeneration policy, UI, BricsCAD runtime, exporter, or GitHub Actions changes.
