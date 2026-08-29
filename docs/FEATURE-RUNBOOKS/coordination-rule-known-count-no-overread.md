# Coordination rule collection known-Count no-overread

Issue: #4473  
Lane-Key: `issue-4473`  
Runtime: `NOT_APPLICABLE` — deterministic Core integrity only.

## Contract

`CoordinationRuleCollectionContract.MaterializeBounded<T>` is the immutable materialization boundary used by coordination rule/profile inputs. Deterministic collection cardinality is admission evidence, not permission to read an element beyond that admitted boundary.

Traversal must execute in this order for each successful advance:

1. `MoveNext()`;
2. reject an item beyond an admitted known Count;
3. reject an item beyond the 10,000-entry pure-streaming ceiling;
4. only then read `Current`, retain it, and increment the observed count.

The materializer must also preserve the existing fail-closed contracts: negative/conflicting/oversized Count refusal before traversal, exact under-yield rejection, post-traversal Count rebinding/stability, and immutable snapshot publication only after successful validation.

## Deterministic acceptance

`CoordinationRuleCollectionKnownCountNoOverreadSmoke` proves:

- Count=N with N+1 yielded items performs the rejecting N+1 `MoveNext()` but never reads N+1 `Current`;
- a 10,001-item pure stream reads exactly 10,000 `Current` values before refusing the overflow;
- counted under-yield remains rejected;
- post-traversal Count drift remains rejected;
- conflicting and negative Count evidence is rejected before traversal;
- stable counted and pure-streaming inputs remain accepted.

`scripts/preflight-coordination-rule-known-count-no-overread.py` pins the explicit-enumerator ordering and smoke registration so a future `foreach` regression cannot silently restore the extra `Current` read.

## Validation

Run repository automatic Shared CI. The carrier is complete only after fresh exact-head validation, reconciliation with latest protected `main`, protected PR `preflight` + `core` success, expected-head merge, and exact-main verification.

Hosted CI is not licensed BricsCAD runtime evidence; no `LOCAL_PASS` claim applies to this Core-only correction.
