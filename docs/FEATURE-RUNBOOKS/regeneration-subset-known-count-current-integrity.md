# Regeneration subset known-Count / `Current` integrity

## Scope

This runbook covers the Core-only `RegenerationEngine.RegenerateDirtySubset(...)` caller-input boundary. It does not change dependency ordering, quantity rules, native BricsCAD behavior, generated ownership, or licensed runtime acceptance.

## Integrity contract

`CanonicalTargetIds(...)` may accept arbitrary `IEnumerable<string>` target IDs. When the source also exposes a supported collection Count, that Count is admission evidence and must be treated as a caller-controlled cardinality contract throughout traversal, not only at admission/finalization.

The traversal ordering is:

1. read and validate every supported known Count source, rejecting negative/conflicting evidence;
2. acquire one enumerator;
3. before each `MoveNext()`, re-read all admitted Count surfaces and require the exact admitted Count contract;
4. after every successful `MoveNext()`, re-read the Count surfaces again **before** any `IEnumerator.Current` access;
5. reject known-Count overrun before reading `Current`;
6. only for admitted values, read `Current`, then apply existing blank/canonical/duplicate validation and the independent project-element maximum in its historical precedence order;
7. after enumeration, reject known-Count under-yield and perform a final exact Count rebound.

The post-`MoveNext()` rebound is required because caller code can mutate Count as a side effect of `MoveNext()` and restore it from `Current`; admission plus final validation alone would miss that transient drift after already consuming an inadmissible value.

Pure streaming inputs remain supported. Their project-cardinality behavior and duplicate-validation precedence remain unchanged from the completed subset-bound contract.

## Deterministic regression

`RegenerationSubsetKnownCountCurrentIntegritySmoke` records `MoveNext()` and `Current` independently and covers:

- stable known-Count overrun: Count=1 with two yielded values rejects the second value before its `Current`;
- stable known-Count under-yield and terminal Count drift;
- `MoveNext()`-induced transient Count growth and shrink, both rejected with zero `Current` reads for the affected item;
- transient negative Count and cross-interface generic/read-only Count conflict, also rejected before `Current`;
- exact stable counted compatibility;
- pure-streaming compatibility.

`scripts/preflight-regeneration-subset-known-count-current-integrity.py` is auto-discovered by aggregate feature guards. It pins `Count rebound -> MoveNext -> Count rebound -> known-Count overrun -> Current`, requires final rebound, and preserves the existing project-bound diagnostic order rather than weakening historical coverage.

## Validation boundary

Required remote validation is the discovered feature preflight, Core Release build/deterministic smoke, and applicable shared/protected CI. BricsCAD V25/V26 licensed runtime is **NOT_APPLICABLE** to this deterministic Core input-integrity correction; hosted build evidence must not be reported as `LOCAL_PASS`.