# Regeneration subset known-Count / `Current` integrity

## Scope

This runbook covers the Core-only `RegenerationEngine.RegenerateDirtySubset(...)` caller-input boundary. It does not change dependency ordering, quantity rules, native BricsCAD behavior, generated ownership, or licensed runtime acceptance.

## Integrity contract

`CanonicalTargetIds(...)` may accept arbitrary `IEnumerable<string>` target IDs. When the source also exposes a supported collection Count, that Count is admission evidence and must be treated as a caller-controlled cardinality contract.

The traversal ordering is:

1. read and validate supported known Count sources, rejecting negative/conflicting evidence;
2. acquire one enumerator;
3. for each successful `MoveNext()`, reject **known-Count** overrun before reading `IEnumerator.Current`;
4. only for admitted values, read `Current`, then apply existing blank/canonical/duplicate validation and the independent project-element maximum in its historical precedence order;
5. after enumeration, reject known-Count under-yield;
6. when traversal cardinality matches admission, re-read the supported Count sources and reject post-traversal source/value drift.

Pure streaming inputs remain supported. Their project-cardinality behavior and duplicate-validation precedence remain unchanged from the completed subset-bound contract.

## Deterministic regression

`RegenerationSubsetKnownCountCurrentIntegritySmoke` uses a hostile `IReadOnlyCollection<string>` that records Count reads, `MoveNext()` calls, and `Current` reads independently. The key overrun assertion is Count=1 with two yielded values: the second successful `MoveNext()` is observed, but the second `Current` must never be read.

The smoke also covers known-Count under-yield, post-traversal Count drift, and exact-count compatibility. Existing `RegenerationSubsetTargetBoundSmoke` remains authoritative for streaming project-cardinality and duplicate-precedence behavior.

`scripts/preflight-regeneration-subset-known-count-current-integrity.py` is auto-discovered by aggregate feature guards and pins the explicit-enumerator ordering without weakening the existing project-bound diagnostic order.

## Validation boundary

Required remote validation is the discovered feature preflight, Core Release build/deterministic smoke, and applicable shared/protected CI. BricsCAD V25/V26 licensed runtime is **NOT_APPLICABLE** to this deterministic Core input-integrity correction; hosted build evidence must not be reported as `LOCAL_PASS`.
