# Work claim — DependencyGraph dirty-order input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-dirty-order-input-freshness`
- Registered: `2026-08-12T14:09:00+07:00`
- Completed: `2026-08-12T14:17:00+07:00`
- Baseline main SHA: `6450b35f25ecc268c2ed09ec9e3707f4092bc1b7`
- Claim merge SHA: `af4ef3a84a7e3d908e3239a34bcfd559e7fd31cc`
- Implementation SHA: `81653ee5b12f793685c9690babcd4e2bd8a20ebf`
- Contract-alignment SHA: `07adbc599c58cff78b47c29211b37a8b738250c5`
- Implementation PRs: `#929`, `#930`
- Priority: P1 — make dirty ordering observe one stable post-enumeration dirty-state snapshot instead of caller-controlled intermediate states.

## Confirmed defect

`DependencyGraph.TopologicalDirtyOrder(IEnumerable<ProjectElement>)` previously validated each element and read `element.Dirty` inside the caller-controlled input enumeration. An iterator could therefore mutate the dirty state of an element that was already yielded before yielding a later element. The returned dirty subset could reflect an intermediate state rather than the state after the input sequence had been completely materialized.

Concrete counterexample: yield element A while A is clean, then call public `A.MarkDirty(...)`, then yield element B. The previous implementation permanently excluded A because it sampled `A.Dirty` before the iterator mutation, even though A was dirty when enumeration completed.

## Implemented contract

- `TopologicalDirtyOrder()` now fully materializes the caller enumerable before dependency validation or dirty-flag sampling.
- The fully materialized input is dependency-validated in a separate phase before any `Dirty` flag is sampled.
- The dirty subset is then built from post-enumeration state and passed through the existing duplicate-ID and iterative topological ordering logic.
- Null-element, dependency validation, duplicate dirty-element ID, cycle detection, and subset-only dependency traversal behavior remain unchanged.
- `DependencyGraphDirtyOrderInputFreshnessSmoke` covers an iterator that dirties an already-yielded element before yielding the next one and a stable dirty subset that preserves dependency-before-dependent ordering while excluding clean input.

## Validation

- Claim-only PR `#928` squash-merged as `af4ef3a84a7e3d908e3239a34bcfd559e7fd31cc` before source changes.
- Implementation PR `#929` changed exactly `DependencyGraph.cs` plus the focused smoke file and squash-merged as `81653ee5b12f793685c9690babcd4e2bd8a20ebf`.
- Follow-up PR `#930` changed only `DependencyGraph.cs` by `+3/-1` to make validation a complete phase before dirty sampling, matching the claim wording exactly; it squash-merged as `07adbc599c58cff78b47c29211b37a8b738250c5`.
- Main readback after concurrent unrelated commits confirms the final three-phase source logic and the focused smoke file are both present.
- No GitHub Actions were invoked. No BricsCAD runtime/build PASS is claimed.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to `TopologicalDirtyOrder()` input materialization / dirty-state snapshot timing
- `tests/QS3D.Core.SmokeTests/DependencyGraphDirtyOrderInputFreshnessSmoke.cs`
- this claim file

## Excluded scope

- `DependencyGraph.Rebuild()` freshness semantics.
- Dependency mutation/cycle validation beyond the existing contract.
- Persistence, regeneration policy, UI, BricsCAD runtime, exporter, or GitHub Actions changes.
