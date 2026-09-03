# Zone assignment Count integrity

Active successor Issue: #5139  
Lane-Key: `issue-5139`

## Boundary

`ProjectZoneService.Assign(...)` accepts caller-controlled `IEnumerable<ProjectElement>` assignment targets. Sources may expose a supported known `Count` through generic, read-only, or non-generic collection interfaces, or may be streaming with no known Count.

A known Count is an admission and traversal integrity contract. The service rebinds supported Count evidence immediately before and after caller-controlled `GetEnumerator()` acquisition, before and after every `MoveNext`, immediately after `Current`, and again after traversal. A changed, negative, conflicting, missing, under-yielding, or over-yielding counted source fails closed before Zone assignment mutation.

After each successful `MoveNext`, the assignment path enforces both the admitted known Count and the 10,000-entry hard limit before reading `IEnumerator.Current`. This prevents an N+1 source value from executing caller-controlled `Current` logic after the admitted cardinality or hard cap has already been exceeded.

Streaming sources remain supported. They have no known-Count gate, but the same hard cap is applied after `MoveNext` and before `Current`, so entry 10,001 is detected without dereferencing its value.

## Transient drift

Final-only Count rebinding is insufficient for hostile enumerables. A source can temporarily change Count during `GetEnumerator`, `MoveNext`, or `Current`, then restore the admitted value before the next ordinary observation. The traversal therefore treats each caller-controlled enumerator boundary as an integrity checkpoint rather than relying on one post-traversal comparison.

The ordering is intentionally explicit:

1. admitted Count snapshot;
2. Count rebound → `GetEnumerator` → Count rebound;
3. Count rebound → `MoveNext` → Count rebound;
4. known-count overrun and hard-cap gates;
5. `Current` → Count rebound;
6. semantic ownership/identity validation and retention;
7. final cardinality plus Count rebound before mutation.

Project `ChangeVersion` checks remain authoritative around Count admission, target traversal, and final Count rebinding. Current Zone/Element ownership is revalidated after traversal and before mutation.

## Deterministic regression

`ZoneAssignmentCountIntegritySmoke` locks these cases:

- Count=1 with two yielded targets: second `MoveNext` is observed, second `Current` is never read;
- a counted source whose Count changes after traversal is rejected before assignment;
- acquisition-time Count drift is rejected after `GetEnumerator` and before first `MoveNext`;
- transient `MoveNext` Count drift is rejected before `Current` can execute;
- transient `Current` Count drift is rejected before any project mutation;
- stable counted target remains accepted and assigned while Count is rebound throughout traversal;
- streaming one-item target remains accepted and assigned;
- streaming 10,001-entry source observes the 10,001st `MoveNext` while `Current` is read only 10,000 times.

The existing `ProjectZoneAssignmentKnownCountTraversalSmoke` continues to cover under-yield, over-yield, in-bound conflicting multi-interface Count evidence, stable counted input, and pure streaming input.

No licensed BricsCAD runtime is required for this Core/domain contract.