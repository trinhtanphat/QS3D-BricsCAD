# Frozen estimate known-Count stability

Status: `SOURCE_READY`

Lane-Key: `issue-4708`

## Purpose

`FrozenEstimateProjection` is the immutable commercial snapshot used to detach estimate state from mutable caller collections. When an input exposes deterministic Count metadata, that Count is part of the admission contract for the whole traversal rather than a hint checked only at admission and after materialization.

## Source contract

- Supported `ICollection<EstimateLine>`, `IReadOnlyCollection<EstimateLine>`, and non-generic `ICollection` Count surfaces are validated before enumeration.
- Negative, conflicting, or greater-than-10,000 admitted Count evidence fails closed before traversal.
- Every counted traversal iteration rebinds the admitted Count before consuming caller `Current`; transient mid-traversal drift therefore fails before semantic validation/materialization.
- The first unexpected line beyond an admitted Count fails before null validation, duplicate-ID validation, row projection, or any later source read.
- After each detached row is materialized, counted inputs rebind Count again; transient post-row drift cannot be hidden by restoring the original Count before traversal finishes.
- A source yielding fewer rows than its admitted Count still fails after traversal completes.
- After an exact traversal, supported Count evidence is read again. Negative, conflicting, or changed post-traversal Count metadata fails before deterministic sorting or result publication.
- Pure streaming sources without Count evidence retain the independent 10,000-line maximum and existing max-plus-one behavior.
- Honest inputs retain case-insensitive duplicate rejection, deterministic sorting by estimate-line identity, detached read-only rows, and exact copied measurement/rate/commercial provenance.

The change hardens input stability only. It does not recalculate rates, quantities, adjustments, currencies, estimate identities, or final amounts.

## Deterministic regression

`FrozenEstimateProjectionTraversalCountSmoke` covers:

1. under-yield after a complete traversal;
2. Count overrun where the first unexpected line is null, proving cardinality drift outranks unexpected-line validation and traversal stops immediately;
3. Count changing only after an exact traversal;
4. Count becoming negative only after an exact traversal;
5. transient mid-traversal Count drift caught before the first caller `Current` read even when later Count evidence would return to the admitted value;
6. transient post-row Count drift caught immediately after detached row materialization;
7. honest counted input with admission, per-row, and post-traversal Count binding;
8. pure streaming input preserving canonical ordering.

The auto-discovered `preflight-frozen-estimate-known-count-stability.py` locks the production ordering and regression registration so later validation changes cannot silently move Count checks behind semantic/materialization work.

## Validation and runtime boundary

This lane is Core/commercial data integrity. Required merge evidence is exact-current protected `preflight` and `core`, including deterministic Core smoke and the normal hosted V25 compile tier selected by shared CI.

There is no licensed BricsCAD runtime acceptance for this package. Hosted CI, source guards, builds, smoke tests, or merge evidence must not be represented as `LOCAL_PASS`.