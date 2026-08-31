# Reporting row provenance traversal integrity

Issue: #4620 / strengthened by #4868  
Lane-Key: `issue-4620` / `issue-4868`

## Purpose

`ReportingRowProvenance.AppendSourceHandles(...)` aggregates stored semantic source handles into report-row provenance used by multiple reporting surfaces. Source handles are caller-owned enumerable state, while the target row is publication state. Traversal failure must therefore be atomic with respect to the target.

## Defect boundary

The original traversal-integrity implementation validates each source around `MoveNext` and stages values before atomic publication. A remaining boundary existed after caller-controlled `Current`: target stability was rebound immediately, but admitted known Count was not rebound until the next loop edge. A counted source could therefore change its Count from `Current` and have the just-read item normalized, validated, deduplicated, and staged before the Count drift was rejected.

The canonicality package remains authoritative for stored-handle content rules. This package does not relax or normalize those rules; it strengthens traversal, cardinality, and publication integrity.

## Production contract

For every append operation:

1. Snapshot existing target handle identities before consuming source values.
2. Bind any supported generic, read-only, or non-generic known Count and require it to be non-negative, mutually consistent, and no greater than 10,000 entries.
3. Revalidate admitted known Count immediately before caller-controlled `MoveNext` and again after a successful `MoveNext` before `Current`.
4. Enforce both admitted Count and the 10,000-entry streaming cap before `Current`, so an N+1 value is never dereferenced after its cardinality is already rejected.
5. Immediately after each successful `Current`, revalidate both target stability and admitted known Count before any normalization, canonical-content validation, duplicate detection, staging, or index increment.
6. Validate nonblank/trimmed canonical handle content and duplicate normalized identity against both existing target values and handles staged in the current append.
7. Rebind/stabilize known Count after traversal and require exact cardinality for counted sources.
8. Publish staged handles to the target only after all source traversal and validation succeeds.

The post-`Current` rebound makes Count drift take precedence over validation of the caller-controlled item that caused the drift. This guarantees zero partial target publication for source traversal, malformed-entry, duplicate-identity, Count-integrity, and streaming-cap failures. Pure streaming enumerables remain supported.

## Deterministic regression

`ReportingRowProvenanceTraversalIntegritySmoke` retains the historical hostile traversal matrix proving:

- a late malformed entry publishes nothing;
- a late duplicate identity publishes nothing;
- an iterator that throws after its first value publishes nothing;
- known Count N+1 is rejected before the extra `Current` read;
- counted under-yield publishes nothing;
- Count drift after `Current` is rejected before the next `MoveNext`;
- Count drift induced by `MoveNext` is rejected before its corresponding `Current`;
- stable counted and pure-streaming sources retain successful behavior;
- streaming entry 10,001 is detected after `MoveNext` but before `Current`, with zero partial publication.

`ReportingRowProvenanceCurrentCountSmoke` adds the stronger precedence proof: its first `Current` both changes admitted Count and returns a malformed blank value. The required outcome is the known Count drift error, not malformed-entry validation, proving the post-`Current` Count rebound executes before processing or staging that item. It also pins one `MoveNext`, one `Current`, and an unchanged target.

The auto-discovered preflight locks explicit enumerator ordering, immediate post-`Current` target/Count rebounds, staging-before-publication, the hard cap, both regression registrations, and forbids regression to direct per-entry publication.

## Runtime boundary

Licensed BricsCAD runtime is not applicable. This is deterministic Core reporting/provenance behavior and must not be represented as `LOCAL_PASS`.
