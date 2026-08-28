# BCF known-Count early drift

Status: `SOURCE_READY / PENDING_LOCAL`

Lane-Key: `issue-4349`

## Purpose

BCF 3.0 exchange materializes bounded collections at four shared package levels: top-level topics, topic viewpoints, topic comments, and viewpoint components. Corroborated Count evidence must not allow an unexpected value to enter null, duplicate, reference, sorting, or package validation before the cardinality contract fails. At the same time, an isolated single-interface Count witness must not suppress the existing independent streaming package bound.

## Source contract

`BcfIssueExchangeContract.MaterializeBounded<T>` owns the shared collection boundary.

- Negative, conflicting, or larger-than-supported known Count values fail before enumeration.
- Count evidence is considered corroborated when the enumerable exposes more than one supported collection interface with agreeing Count values.
- For corroborated Count evidence, the first value yielded after that Count is exhausted fails before the value is appended to the materialized list.
- A source that yields fewer values than any known Count still fails after traversal completes.
- A single-interface Count witness remains subject to the pre-existing streaming maximum first; a dishonest `IReadOnlyCollection<T>` reporting Count=1 while yielding beyond the package maximum still stops at item 257/1025/1001 with the package-bound diagnostic.
- Sources with no known Count retain the same independent package maximum bound.
- Honest counted inputs and pure streaming inputs retain canonical sorting and existing BCF identity/reference validation.

This distinction preserves the established collection-bound contract while giving stronger, corroborated cardinality evidence early precedence over validation of an unexpected nested BCF object.

## Deterministic regression

`BcfIssueExchangeKnownCountEarlyDriftSmoke` uses a collection exposing `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` with agreeing Count values. It covers:

1. topics: Count=1 with a valid topic followed by an unexpected null;
2. viewpoints: Count=1 with a valid viewpoint followed by an unexpected null;
3. comments: Count=1 with a valid comment followed by an unexpected null;
4. components: Count=1 with a valid component followed by an unexpected null;
5. under-yield: Count=2 with one topic, rejected only after completed traversal;
6. pure streaming topics: no Count metadata, still accepted and canonically sorted.

The overrun regressions assert exactly two `MoveNext` calls: the expected item and the first unexpected item. No later value may be requested or materialized after corroborated drift is proven.

`BcfIssueExchangeCollectionBoundSmoke` remains part of the contract and explicitly protects single-interface dishonest Count behavior and the existing topic/viewpoint/comment/component streaming maxima.

## Validation boundary

This package is Core BCF/export data-integrity work. It changes no BCF schema, IFC GUID representation, ZIP/archive I/O, native CAD adapter, or release artifact.

There is no licensed BricsCAD runtime acceptance in this source lane. `PENDING_LOCAL` is retained only as an explicit non-claim boundary: hosted CI, deterministic smoke, or protected merge must never be represented as `LOCAL_PASS`.

Merge requires the canonical current-main candidate to pass protected `preflight` and `core`, followed by expected-head merge and exact-main verification.
