# Regeneration subset known-Count / `Current` integrity

## Scope

This runbook covers the Core-only `RegenerationEngine.RegenerateDirtySubset(...)` caller-input boundary. It does not change dependency ordering, quantity rules, native BricsCAD behavior, generated ownership, or licensed runtime acceptance.

## Integrity contract

`CanonicalTargetIds(...)` may accept arbitrary `IEnumerable<string>` target IDs. When the source also exposes a supported collection Count, that Count is admission evidence and must be treated as a caller-controlled cardinality contract.

The traversal ordering is:

1. read and validate supported known Count sources, rejecting negative/conflicting evidence;
2. acquire one enumerator;
3. for each successful `MoveNext()`, reject known-Count overrun and the independent project-element maximum **before** reading `IEnumerator.Current`;
4. only for admitted values, read `Current`, then apply blank/canonical/duplicate validation and retain the ID;
5. after enumeration, reject known-Count under-yield;
6. re-read the supported Count sources and reject post-traversal source/value drift.

Pure streaming inputs remain supported and are bounded independently by the current project element count.

## Deterministic regression

`RegenerationSubsetKnownCountCurrentIntegritySmoke` uses hostile enumerables that record Count reads, successful/terminal `MoveNext()` attempts, and `Current` reads independently. The key overrun assertion is Count=1 with two yielded values: the second `MoveNext()` is observed, but the second `Current` must never be read.

The smoke also covers streaming project-bound overrun before `Current`, known-Count under-yield, post-traversal Count drift, and exact-count compatibility.

`scripts/preflight-regeneration-subset-known-count-current-integrity.py` is auto-discovered by aggregate feature guards and pins the explicit-enumerator ordering plus the hostile regression surface.

## Validation boundary

Required remote validation is the discovered feature preflight, Core Release build/deterministic smoke, and applicable shared/protected CI. BricsCAD V25/V26 licensed runtime is **NOT_APPLICABLE** to this deterministic Core input-integrity correction; hosted build evidence must not be reported as `LOCAL_PASS`.
