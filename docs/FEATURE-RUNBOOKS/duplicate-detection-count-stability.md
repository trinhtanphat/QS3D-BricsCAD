# Duplicate detection known-Count stability

## Scope

This contract protects both public `DuplicateDetectionService.Detect` input boundaries: direct `DuplicateCandidate` sources and `CoordinationElement` sources projected to candidates.

## Invariant

An exposed collection Count is snapshot evidence. It must remain equal to the admitted Count throughout traversal; it must not be erased by element-to-candidate projection, and drift followed by rebound must still fail closed.

For both overloads, duplicate detection therefore:

1. binds all exposed `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count surfaces before traversal;
2. rejects negative, conflicting, or greater-than-500 known Counts;
3. revalidates known Count before each enumerator advance;
4. after successful advance, revalidates again before reading `Current`;
5. revalidates after traversal terminates and then verifies materialized cardinality;
6. materializes original `CoordinationElement` sources directly so projection cannot erase Count evidence;
7. retains bounded pure-streaming support when no known Count surface exists.

Null elements/candidates, duplicate element identities, deterministic pair ordering, exact/near/semantic classification, options validation, and the 10,000-result bound remain unchanged.

## Deterministic regression

`DuplicateDetectionCountStabilitySmoke` proves hostile candidate-source growth, shrink-before-Current, and terminal rebound rejection; proves the same integrity boundary is retained by the element overload instead of disappearing through generator projection; verifies stable semantic/exact duplicate behavior; and preserves both pure-streaming overloads.

`preflight-duplicate-detection-count-stability.py` pins both explicit materialization paths and their `Count → MoveNext → Count → Current → terminal Count → cardinality` ordering, and rejects restoration of the old `ProjectCandidates` generator boundary.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD runtime. This is deterministic Core coordination integrity and is accepted through source guards, Core build/smoke, and protected PR CI only.
