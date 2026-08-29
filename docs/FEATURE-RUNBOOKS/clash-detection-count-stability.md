# Clash detection known-Count stability

## Scope

This contract protects the deterministic Core snapshot boundary used by `ClashDetectionService.Detect` when the supplied `IEnumerable<CoordinationElement>` also exposes a known cardinality through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`.

## Invariant

A known Count is semantic snapshot evidence, not a one-time capacity hint. Detection must fail closed when an exposed known Count changes while the source is being traversed, even if it later rebounds to the originally admitted value.

The service therefore:

1. binds and validates all exposed known Count surfaces before traversal;
2. rejects negative, conflicting, or greater-than-500 known Counts at admission;
3. revalidates the admitted known Count immediately before each enumerator advance;
4. after a successful advance, revalidates again before reading `Current`;
5. revalidates once more after traversal terminates;
6. still verifies that the materialized element count equals the admitted known Count;
7. preserves support for bounded pure-streaming `IEnumerable<CoordinationElement>` inputs that expose no Count surface.

Existing null-element, duplicate identity, deterministic sorting, clash classification, result-cap and finite-geometry protections remain unchanged.

## Deterministic regression

`ClashDetectionCountStabilitySmoke` uses hostile known-count enumerable implementations to prove that growth is rejected before a second enumerator advance, shrink is rejected before an unsafe `Current` read, final rebound drift is rejected after traversal, stable known-count input still yields the canonical clash result, and pure streaming input remains supported.

`preflight-clash-detection-count-stability.py` pins the production ordering and rejects regression to the previous outer `foreach` form, where the enumerator could advance/read `Current` before known Count stability was rechecked.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD runtime. This is deterministic Core coordination snapshot/integrity behavior and must be validated by Core build/smoke plus repository source guards and protected PR CI.
