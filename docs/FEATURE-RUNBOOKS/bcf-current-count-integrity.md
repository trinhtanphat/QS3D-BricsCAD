# BCF Current-induced Count integrity

`BcfIssueExchangeContract.MaterializeBounded<T>` is the shared bounded materialization boundary for BCF topics, viewpoints, comments, and viewpoint components.

For sources that expose a supported `Count`, the admitted cardinality contract must remain stable for the whole traversal. The ordering for every successful element is intentionally strict:

1. validate/rebind the admitted Count before `MoveNext()`;
2. call `MoveNext()`;
3. rebind Count again before any `Current` read or overrun-sensitive staging;
4. enforce known-count overrun and configured hard-cap checks;
5. read `enumerator.Current`;
6. immediately rebind the exact admitted Count again;
7. only then stage the item and advance the observed count.

The post-`Current` rebound is required because `Current` is caller-controlled code. A collection can mutate one or more supported Count surfaces from that getter even when `MoveNext()` itself was stable. BCF package state must not stage the returned topic/viewpoint/comment/component until the cardinality contract has been revalidated.

The deterministic smoke `BcfIssueExchangeCurrentCountIntegritySmoke` pins both a stable one-topic positive control and a hostile `IReadOnlyCollection<T>` whose `Current` changes Count. The hostile case must fail on the immediate post-Current rebound before a second `MoveNext()` is attempted. The stable case pins the full one-item Count observation budget so accidental removal or relocation of the post-Current rebound is detected.

This is Core/export data-integrity behavior only; no licensed BricsCAD or private-DWG runtime evidence is required.
