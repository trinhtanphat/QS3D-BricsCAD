# Work claim — DependencyGraph processed-input freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:11:00+07:00`
- Baseline main SHA observed: `c300c2db59663b11961fa1b49418d504e763aa58`
- Priority: P1 stateful dependency graph integrity
- Task Key: `CORE-DEPENDENCY-GRAPH-PROCESSED-INPUT-FRESHNESS`

## Confirmed defect

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` validates and stages each yielded element immediately. A caller-controlled lazy enumerable can yield element `A`, let Rebuild consume `A.DependsOn`, then on the next `MoveNext()` mutate `A.DependsOn` before yielding `B`. The current `_rebuildVersion` guard only detects a successful re-entrant `Rebuild()` on the same graph; direct mutation of an already-processed yielded element does not advance that version. The outer call can therefore commit `_dependents` that no longer represents the final state of the exact ProjectElement objects it consumed.

The completed rebuild-input-freshness lane explicitly owns successful re-entrant `Rebuild()` overwrite prevention, not direct mutation of processed `DependsOn` state. Search found no processed-input/DependsOn structural-freshness lane.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs` — verify that dependency identities of already-processed yielded elements remain semantically unchanged before staged graph commit.
- `tests/QS3D.Core.SmokeTests/DependencyGraphProcessedInputFreshnessSmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- Rebuild still stages all graph state before mutation.
- After caller enumeration completes, every processed element must still have the same case-insensitive dependency-id set that was used to stage the graph.
- Added/removed/replaced dependencies or newly malformed dependency state fail closed before replacing the previous graph.
- Dependency-list reordering or casing-only aliases remain semantically equivalent and do not fail solely because of sequence/case differences.
- Preserve existing null/duplicate/missing-dependency validation, re-entrant `_rebuildVersion` protection, lookup/transitive/topological semantics, and previous graph on failure.

## Validation plan

Use a lazy enumerable that yields a valid `A`, mutates `A.DependsOn` only when enumeration resumes for the next item, then yields `B`. Require Rebuild to fail and preserve the prior graph. Add a stable control and a reorder/case-equivalent control. Read back source/test diffs and verify ancestry on moving `main`.

No GitHub Actions dispatch; no executable .NET/full build or licensed BricsCAD V25/V26 runtime PASS claim unless actually executed.
