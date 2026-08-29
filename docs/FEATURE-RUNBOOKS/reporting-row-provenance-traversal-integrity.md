# Reporting row provenance traversal integrity

Issue: #4620  
Lane-Key: `issue-4620`

## Purpose

`ReportingRowProvenance.AppendSourceHandles(...)` aggregates stored semantic source handles into report-row provenance used by multiple reporting surfaces. Source handles are caller-owned enumerable state, while the target row is publication state. Traversal failure must therefore be atomic with respect to the target.

## Defect boundary

The previous implementation validated each handle and immediately appended it to `target` from a `foreach`. A later blank/padded/duplicate handle or an exception from a hostile iterator could fail the report while leaving earlier handles published into the target. It also lacked an explicit source-entry cap and treated collection cardinality as unconstrained enumerable behavior.

The earlier canonicality package remains authoritative for stored-handle content rules. This package does not relax or normalize those rules; it adds traversal, cardinality, and publication integrity.

## Production contract

For every append operation:

1. Snapshot existing target handle identities before consuming source values.
2. Bind any supported generic, read-only, or non-generic known Count and require it to be non-negative, mutually consistent, and no greater than 10,000 entries.
3. Revalidate admitted known Count immediately before caller-controlled `MoveNext` and again after a successful `MoveNext` before `Current`.
4. Enforce both admitted Count and the 10,000-entry streaming cap before `Current`, so an N+1 value is never dereferenced after its cardinality is already rejected.
5. Validate nonblank/trimmed canonical handle content and duplicate normalized identity against both existing target values and handles staged in the current append.
6. Rebind/stabilize known Count after traversal and require exact cardinality for counted sources.
7. Publish staged handles to the target only after all source traversal and validation succeeds.

This guarantees zero partial target publication for source traversal, malformed-entry, duplicate-identity, Count-integrity, and streaming-cap failures. Pure streaming enumerables remain supported.

## Deterministic regression

`ReportingRowProvenanceTraversalIntegritySmoke` invokes the helper through the Core assembly and uses hostile enumerable implementations to prove:

- a late malformed entry publishes nothing;
- a late duplicate identity publishes nothing;
- an iterator that throws after its first value publishes nothing;
- known Count N+1 is rejected before the extra `Current` read;
- counted under-yield publishes nothing;
- Count drift after `Current` is rejected before the next `MoveNext`;
- Count drift induced by `MoveNext` is rejected before its corresponding `Current`;
- stable counted and pure-streaming sources retain successful behavior;
- streaming entry 10,001 is detected after `MoveNext` but before `Current`, with zero partial publication.

The auto-discovered preflight locks explicit enumerator ordering, staging-before-publication, the hard cap, focused regression registration, and forbids regression to direct per-entry publication.

## Runtime boundary

Licensed BricsCAD runtime is not applicable. This is deterministic Core reporting/provenance behavior and must not be represented as `LOCAL_PASS`.
