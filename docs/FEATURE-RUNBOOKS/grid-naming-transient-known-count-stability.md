# Grid naming transient known-Count stability

## Contract

`GridNamingService.Renumber(...)` accepts caller-provided target IDs, including sources that expose generic, read-only, or non-generic `Count` surfaces. When a supported Count is present, the admitted value is part of the Grid renumber transaction boundary.

For every traversal step the implementation must re-read all supported Count surfaces before `MoveNext()` and again after a successful `MoveNext()` but before the admitted-count bound, the 2,000-target hard cap, and semantic `IEnumerator.Current`. Growth, shrink, negative Count, or conflicting Count surfaces must fail closed before any affected target identity is consumed.

The stronger traversal contract supplements rather than replaces existing guarantees: ProjectState `ChangeVersion` freshness, post-traversal Count rebound, under-yield rejection, duplicate/canonical target validation, deterministic numeric/alphabetic numbering, no partial Grid mutation, and pure streaming input support.

## Deterministic regression

`GridNamingTransientKnownCountSmoke` uses adversarial counted sources with independent Count, `MoveNext`, and `Current` instrumentation. Transient growth, shrink, negative Count, and cross-interface conflicts must be rejected with exactly one `MoveNext` and zero semantic `Current` reads, while the project version and Grid naming properties remain unchanged. Stable counted and streaming controls must still renumber successfully.

## Remote validation

Run the standard Shared Branch and Integration CI on the exact branch head. Required source evidence is terminal `preflight=SUCCESS` and `core=SUCCESS`, including deterministic smoke, trusted BricsCAD V25 compile-reference validation, V25 plugin build, and final build. This contract is Core/domain-only; licensed BricsCAD/private-DWG runtime acceptance is not applicable and must not be claimed remotely.
