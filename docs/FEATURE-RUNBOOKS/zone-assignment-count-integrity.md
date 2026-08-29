# Zone assignment Count integrity

Issue: #4611  
Lane-Key: `issue-4611`

## Boundary

`ProjectZoneService.Assign(...)` accepts caller-controlled `IEnumerable<ProjectElement>` assignment targets. Sources may expose a supported known `Count` through generic, read-only, or non-generic collection interfaces, or may be streaming with no known Count.

A known Count is an admission contract, not only a post-hoc diagnostic. After each successful `MoveNext`, the assignment path must enforce the 10,000-entry hard limit and any admitted known Count before reading `IEnumerator.Current`. This prevents an N+1 source value from executing caller-controlled `Current` logic after the admitted cardinality has already been exceeded.

Streaming sources remain supported. They have no known-Count gate, but the same hard cap is applied after `MoveNext` and before `Current`, so entry 10,001 is detected without dereferencing its value.

## Two-phase stability

Supported Count interfaces are snapshotted before traversal. After traversal, the source Count contract is rebound and compared with the admission snapshot. A changed, negative, conflicting, under-yielding, or over-yielding counted source fails closed before any Zone assignment mutation.

Project `ChangeVersion` checks remain authoritative around Count admission, target traversal, and post-traversal Count rebinding. Current Zone/Element ownership is revalidated after traversal and before mutation.

## Deterministic regression

`ZoneAssignmentCountIntegritySmoke` instruments `MoveNext`, `Current`, and Count reads independently and locks these cases:

- Count=1 with two yielded targets: second `MoveNext` is observed, second `Current` is never read;
- exact one-item traversal whose Count drifts 1→2: rejected during post-traversal rebinding before assignment;
- stable counted target: accepted and assigned;
- streaming one-item target: accepted and assigned;
- streaming 10,001-entry source: the 10,001st `MoveNext` is observed while `Current` is read only 10,000 times.

## Carrier admission

The first branch run failed before feature guards because the reservation Issue omitted the exact `Canonical carrier` field required by reservation protocol v2. Issue #4611 now binds the canonical branch explicitly; the same lane is retained and fresh CI must validate the corrected reservation plus this source/test package.

No licensed BricsCAD runtime is required for this Core/domain contract.