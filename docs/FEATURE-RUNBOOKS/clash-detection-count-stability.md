# Clash detection known-Count stability

Issue: #4940  
Lane-Key: `issue-4940`  
Ownership-Key: `core.coordination.clash-current-induced-known-count-stability-v1`

## Scope

This contract protects the deterministic Core snapshot boundary used by `ClashDetectionService.Detect` when the supplied `IEnumerable<CoordinationElement>` also exposes a known cardinality through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`.

## Invariant

A known Count is semantic snapshot evidence, not a one-time capacity hint. Detection must fail closed when an exposed known Count changes while the source is being traversed, including mutation performed by caller-controlled `IEnumerator.Current`.

The service therefore:

1. binds and validates all exposed known Count surfaces before traversal;
2. rejects negative, conflicting, or greater-than-500 known Counts at admission;
3. revalidates the admitted known Count immediately before each enumerator advance;
4. after a successful advance, revalidates again before reading `Current`;
5. reads the accepted `Current` exactly once, then immediately revalidates Count before null validation, duplicate-id acceptance, or snapshot publication;
6. revalidates once more after traversal terminates;
7. still verifies that the materialized element count equals the admitted known Count;
8. preserves support for bounded pure-streaming `IEnumerable<CoordinationElement>` inputs that expose no Count surface.

Existing null-element, duplicate identity, deterministic sorting, clash classification, result-cap and finite-geometry protections remain unchanged.

## Deterministic regression

`ClashDetectionCountStabilitySmoke` proves pre/post-`MoveNext` drift rejection, post-traversal rebound integrity, stable counted behavior, and pure-streaming support. Issue #4940 adds a hostile counted source whose `Current` changes Count from 1 to 2 while returning null. The canonical Count-drift `InvalidOperationException` must win before ordinary null-element validation, and `Current` must be read exactly once.

The stable counted regression pins the stronger exact Count-read budget of nine reads for two accepted elements: admission, pre/post each `MoveNext`, post each `Current`, the terminal pre-advance rebound, and final post-traversal rebound. This budget is intentionally tightened rather than weakened.

`preflight-clash-detection-count-stability.py` pins the production ordering:

`admission -> Count -> MoveNext -> Count -> Current -> Count -> semantic acceptance -> final Count -> exact cardinality`.

It also rejects regression to the previous outer `foreach` form and requires exactly one accepted `Current` read site.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD runtime. This is deterministic Core coordination snapshot/integrity behavior and must be validated by Core build/smoke plus repository source guards and protected PR CI. No hosted result is represented as `LOCAL_PASS`.
