# Work claim — DependencyGraph processed-input freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:11:00+07:00`
- Completed: `2026-08-12T11:14:00+07:00`
- Baseline main SHA observed: `c300c2db59663b11961fa1b49418d504e763aa58`
- Claim commit: `41ec60f899c8aff4f73b9896299050c5579399a5`
- Source fix: `d00d23c9a6ef85f04939b3d2c9de69aaf8cf654f`
- Regression smoke: `f7d13df14b731490217ecae84d1e5116d220290c`
- Priority: P1 stateful dependency graph integrity
- Task Key: `CORE-DEPENDENCY-GRAPH-PROCESSED-INPUT-FRESHNESS`

## Confirmed defect

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` validated and staged each yielded element immediately. A caller-controlled lazy enumerable could yield element `A`, let Rebuild consume `A.DependsOn`, then on the next `MoveNext()` mutate `A.DependsOn` before yielding `B`. The existing `_rebuildVersion` guard only detects a successful re-entrant `Rebuild()` on the same graph; direct mutation of an already-processed yielded element did not advance that version. The outer call could therefore commit `_dependents` that no longer represented the final state of the exact ProjectElement objects it consumed.

The completed rebuild-input-freshness lane owns successful re-entrant `Rebuild()` overwrite prevention. This lane adds the separate processed-object structural freshness guarantee.

## Implemented contract

- Rebuild still stages all graph state before mutation.
- While staging each element, it records the case-insensitive dependency-id set used to build `_dependents`.
- After caller enumeration completes, before missing-reference validation or graph commit, every processed element is revalidated and compared against its staged dependency set.
- Added/removed/replaced dependency identities fail closed before replacing the prior graph.
- Newly blank/noncanonical/duplicate dependency state also fails through the existing validation boundary.
- Reordering and casing-only aliases remain semantically equivalent because comparison is count + case-insensitive set membership, not sequence/exact-case equality.
- Existing null/duplicate/missing-dependency validation, re-entrant `_rebuildVersion` protection, direct/transitive lookup and topological logic are unchanged.

## Regression coverage

`DependencyGraphProcessedInputFreshnessSmoke` is auto-registered and covers:

- lazy mutation after element `A` has already been processed, requiring `Rebuild()` to fail closed;
- failed rebuild preserving the complete previous graph and not leaking staged elements;
- reorder + casing-only mutation remaining accepted with equivalent graph semantics;
- stable input preserving direct and transitive dependency results.

## Validation evidence

- Exact source readback for `d00d23c9a6ef85f04939b3d2c9de69aaf8cf654f` shows only the processed dependency snapshot + revalidation block in `DependencyGraph.Rebuild(...)`.
- Exact regression readback for `f7d13df14b731490217ecae84d1e5116d220290c` shows one new focused 123-line smoke source.
- Compared source fix to observed `main` `f7d13df14b731490217ecae84d1e5116d220290c`: `ahead_by=3`, `behind_by=0`, source fix is the merge base, and no later commit in that range modified `src/QS3D.Core/Services/DependencyGraph.cs`.
- No GitHub Actions were dispatched. Smoke source was committed/read back but not executed from this connector-only session. No executable .NET/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: `DependencyGraph.Rebuild(...)` can no longer publish a graph from stale `DependsOn` state when a lazy input mutates an already-processed element during enumeration.
