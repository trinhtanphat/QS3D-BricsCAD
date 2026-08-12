# Work claim — DependencyGraph rebuild input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-graph-rebuild-input-freshness`
- Registered: `2026-08-12T10:41:00+07:00`
- Completed: `2026-08-12T10:44:00+07:00`
- Baseline main SHA: `1fc1a279f71c7a31e514f97ae75c11116d7f4ac7`
- Priority: P1 — fail-closed stateful graph rebuild at a caller-controlled reentrant enumeration boundary.

## Confirmed defect

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` materialized a new dependency graph from caller-controlled lazy input and then replaced the current `_dependents` / `_elementsById` state. During enumeration, the producer could reentrantly call `Rebuild()` on the same `DependencyGraph`. The inner rebuild could complete and install a newer graph, after which the outer rebuild resumed and overwrote that newer state using stale materialized input.

## Implemented contract

- Added a private monotonic `_rebuildVersion` tracking successful graph rebuilds.
- `Rebuild()` captures the revision immediately before caller enumeration.
- Existing null/duplicate/dependency and missing-dependency validation remains before graph application.
- After validation, revision drift is rejected before clearing or replacing `_dependents` or `_elementsById`.
- The checked next revision is prepared before the first graph mutation and applied only after both dictionaries are replaced.
- Every successful rebuild advances the private revision, including content-equivalent rebuilds, so any successful inner rebuild invalidates a stale outer reentrant rebuild.
- Direct/transitive lookup and topological-order logic are unchanged.

## Regression coverage

Focused Core smoke source covers:

- stable lazy rebuild preserving direct/transitive lookup and element resolution;
- successful inner rebuild during outer enumeration, with the outer call failing closed and preserving the newer inner graph;
- successful inner rebuild followed by an empty outer enumeration, proving the stale outer call cannot clear the newer graph.

A `ModuleInitializer` registration and static preflight lock revision capture/enumeration/freshness/apply ordering, checked revision preparation, existing missing-dependency validation precedence, smoke cases, and registration.

## Evidence

- Claim registration: `eb8ee38858cdf5256cc81ad098a2d83434ecf473`
- Plan: `c6872b57cde9acc8af1d2b12a6a5a10171f523c9`
- Source fix: `d78f24393c7cb43972efa1d3ff32916013fa12a8`
- Smoke regression: `9fcd0afd4f1caef82e8bd2d42bee7982c6e1c753`
- Smoke registration: `2a4f911f8a91b913ee261f8503a583a992ac89e6`
- Static preflight: `20f17b8de57cd48cc8bda803869555e21735afd0`
- Latest-main readback confirmed source, smoke, registration, and preflight content after concurrent repository changes.

## Validation limitations

The Core smoke executable and Python preflight were not executed in this connector-only environment. No GitHub Actions/build/release dispatch or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope

- Cross-thread synchronization/thread-safety guarantees.
- `ProjectState.ChangeVersion` semantics.
- Dependency validation/topological algorithm changes unrelated to rebuild freshness.

## Completion

`COMPLETED`: a successful reentrant rebuild performed while caller-controlled outer rebuild elements are being enumerated can no longer be overwritten by the stale outer `Rebuild()` operation.
