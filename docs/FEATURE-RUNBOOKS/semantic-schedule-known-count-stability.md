# Semantic schedule known-Count stability

This runbook covers `SemanticScheduleDefinition.SnapshotBounded<T>` in `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs`.

## Integrity contract

When an input exposes a supported known `Count` surface, the admitted cardinality is part of the semantic schedule snapshot contract for the entire traversal. The snapshot must fail closed if that evidence changes or conflicts:

- immediately before caller-controlled `MoveNext()`;
- immediately after `MoveNext()` returns;
- immediately after caller-controlled `Current` is read and before the value is retained;
- after traversal before publication.

The capacity and over-yield guards remain before `Current`, so the first unsupported item is never read. Exact known cardinality, negative/conflicting Count rejection, the 5,000-id/category and 32-column bounds, defensive snapshots, and pure-streaming inputs remain supported as before.

## Deterministic regression

`SemanticScheduleDefinitionBoundedSnapshotSmoke` includes hostile `IReadOnlyCollection<string>` sources that temporarily alter Count during `MoveNext()` or `Current` and restore it before legacy post-traversal validation would observe the drift. The MoveNext case must fail before `Current` is read; the Current case must fail before the returned value is retained. A stable counted source remains accepted.

`scripts/preflight-semantic-schedule-definition-bounds.py` pins the ordering:

`pre-MoveNext Count -> MoveNext -> post-MoveNext Count -> terminal/bounds/over-yield -> Current -> post-Current Count -> retain`

## Validation

Run the repository Shared CI for the exact candidate. Required remote evidence is current exact-head protected `preflight` and `core` success, including deterministic smoke and the existing V25 compile-reference/plugin-build stages. This is a deterministic Core documentation contract and does not constitute licensed BricsCAD runtime `LOCAL_PASS`.